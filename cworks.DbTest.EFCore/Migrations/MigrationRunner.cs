using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace cworks.DbTest.EFCore.Migrations
{


    /// <summary>
    /// Runs EF Data Migrations, ensuring only 1 migration is run at a time.
    /// This is useful in environments where migrations are run at application startup
    /// and there are multiple instance of the application running simultaneously.
    ///
    /// This implementation relies on exclusive table lock of the self provisioned __EFMigrationHistoryLock table.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DataMigrationRunner
    {
        private readonly ILogger<DataMigrationRunner> logger;
        private readonly IServiceProvider serviceProvider;
        private string dataContextConnectionString;
        private string dataContextDbName;


        private const string LockTableName = "__EFMigrationHistoryLock";

        public DataMigrationRunner(ILogger<DataMigrationRunner> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;

        }


        public void ApplyMigrations<T>()
        {
            ApplyMigrations(typeof(T));
        }

        public void ApplyMigrations(Type contextType)
        {
            this.logger.LogInformation($"Applying Pending EF Data Migrations for {contextType.Name}");

            var dataContext = this.serviceProvider.CreateScope().ServiceProvider.GetService(contextType);
            var ctx = dataContext as DbContext ?? throw new ArgumentNullException(nameof(dataContext), "must derive from DbContext");

            var conn = ctx.Database.GetDbConnection();
            this.dataContextConnectionString = conn.ConnectionString;
            this.dataContextDbName = conn.Database;

            EnsureDatabase();
            // make sure the semaphore lock table exists in the db
            EnsureTransactionLockTable();


            // create an execution strategy to avoid azure retries if the lock fails
            IExecutionStrategy strategy = ctx.Database.CreateExecutionStrategy();
            strategy.Execute(() =>
            {
                // start the transaction 
                using (ctx.Database.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    // acquire the distributed lock 
                    if (AcquireLock(ctx))
                    {
                        // anything that happens here will only happen on 
                        // a single instance of the application service at one time.

                        // Apply any pending EF migrations
                        RunMigrations(contextType, this.serviceProvider);

                        // Release the lock
                        ReleaseLock();
                        this.logger.LogInformation($"EF Data Migrations for {contextType.Name} has completed.");
                    }
                }
            });

        }

        private bool AcquireLock(DbContext ctx)
        {
            try
            {
                this.logger.LogDebug("DataMigrationsRunner - Waiting to acquire distributed lock...");
                string sqlQuery = $"SELECT Top 1 * FROM {LockTableName} WITH (XLOCK) order by Id";
                ctx.Database.ExecuteSqlCommand(sqlQuery);
                this.logger.LogDebug("DataMigrationsRunner - Distributed lock acquired.");
                return true;
            }
            catch (Exception e)
            {
                this.logger.LogError(e, "DataMigrationsRunner - Failed to acquire distributed lock. Migrations will not be run.");
                return false;
            }

        }

        private void ReleaseLock()
        {
            // really nothing to do here, the db transaction going out of scope
            // is what actually releases the lock on the table. 

            // we'll add logging here to tell a good story for diagnostics
            this.logger.LogDebug("DataMigrationsRunner - Distributed lock released");
        }


        private void RunMigrations(Type dataContextType, IServiceProvider servicesProvider)
        {
            this.logger.LogDebug("DataMigrationsRunner - Preparing to Run migrations.");
            // Migrations can't execute on a connection that is participating in a 
            // transaction, so we need to spin up another instance of the DB context
            // and run migrations on that instance.
            var migrationContext = ProduceMigrationContext(dataContextType, servicesProvider);
            if (migrationContext != null)
            {
                migrationContext.Database.Migrate();
                this.logger.LogDebug("DataMigrationsRunner - Migrations have been run.");
            }
            else
            {
                this.logger.LogError($"DataMigrationsRunner - Failed to run migrations.  Failed to create instance of {dataContextType.Name} from scoped service provider.");
            }

        }
        private DbContext ProduceMigrationContext(Type dataContextType, IServiceProvider servicesProvider)
        {
            return servicesProvider.CreateScope().ServiceProvider.GetService(dataContextType) as DbContext;
        }


        /// <summary>
        /// ensures the table we'll be using to lock execution to a single instance
        /// actually exists in the DB
        /// </summary>
        private void EnsureTransactionLockTable()
        {

            this.logger.LogDebug("Ensuring Migration Transaction Lock Table...");
            try
            {
                var sql = $"IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = N'{LockTableName}')";
                sql = sql + "BEGIN ";
                sql = sql + $" CREATE TABLE {LockTableName} (id int null);";
                // Really Important - the lock won't be respected if the table is empty
                // so add a row to the table to give sql server a data page to lock.
                sql = sql + $" INSERT INTO {LockTableName} VALUES (1);";
                sql = sql + "END";
#pragma warning disable EF1000
                ExecuteNonQuery(sql, this.dataContextConnectionString);
                //ctx.Database.ExecuteSqlCommand(sql);
                this.logger.LogDebug("Successfully Ensured Migration Transaction Lock Table.");
            }
            catch (Exception e)
            {
                //possible race condition -- just eat the exception
                this.logger.LogError(e, "Error ensuring Migration Transaction Lock Table.");
            }
        }

        public void EnsureDatabase()
        {


            if (!DoesDbExist())
            {
                CreateDatabase();
                // re-try to access the db now. 
                if (!DoesDbExist())
                {
                    throw new Exception("Fatal Exception - Failed to initialize data context.");
                }
            }
        }

        private void CreateDatabase()
        {
            this.logger.LogDebug("Creating new database...");
            var connStr = this.dataContextConnectionString.Replace(this.dataContextDbName, "master");
            ExecuteNonQuery($"Create Database [{this.dataContextDbName}]", connStr);

        }
        private bool DoesDbExist()
        {
            var connStr = $"{this.dataContextConnectionString}".Trim();
            connStr = connStr + (!connStr.EndsWith(";") ? "; " : "") + "Connection Timeout =3";
            try
            {

                using (var conn = new SqlConnection(connStr))
                {

                    conn.Open();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                SqlConnection.ClearPool(new SqlConnection(connStr));
                SqlConnection.ClearPool(new SqlConnection(this.dataContextConnectionString));
            }
        }


        private void ExecuteNonQuery(string sql, string connectionString)
        {
            using (var conn = new SqlConnection(connectionString))
            {

                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
            }
        }



    }

    [ExcludeFromCodeCoverage]
    public static class DataMigrationRunnerExtensions
    {
        public static IServiceCollection AddDataMigrationRunner(this IServiceCollection services)
        {
            services.TryAddScoped<DataMigrationRunner>();
            return services;
        }
    }
}
