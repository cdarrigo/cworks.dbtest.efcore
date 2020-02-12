using System;
using cworks.DbTest.EFCore.Migrations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace cworks.DbTest.EFCore
{
    public interface IEfDbScaffolder:IDbScaffolder
    {
        void RegisterServices<TDataContext>(IServiceCollection services, IDbTestRunnerContext context, DbTestRunnerConfiguration config) where TDataContext : DbContext;
    }

    public class EfDbScaffolder:SqlServerDbScaffolder, IEfDbScaffolder
    {

        /// <summary>
        /// Participates in the registration of services
        /// during DbTestRunner startup.
        ///
        /// Provides an opportunity to inject data related
        /// services into the IoC container. 
        /// </summary>
        public void RegisterServices<TDataContext>(IServiceCollection services, IDbTestRunnerContext context, DbTestRunnerConfiguration config) where TDataContext : DbContext
        {
            // produce and register the data context.
            ConfigureDataContext<TDataContext>(services, context, config);
        }

        
        /// <summary>
        /// Configures all the services necessary to produce the data context from the IoC container. 
        /// </summary>
        private void ConfigureDataContext<TDataContext>(IServiceCollection services, IDbTestRunnerContext context, DbTestRunnerConfiguration config) where TDataContext : DbContext
        {

            // Our data contexts require loggers, so add the loggers to the services collection
            services.AddLogging();

            // if a db name is specified, use that, otherwise generate a unique database name
            context.DbName = context.DbName?? config.DbName ?? ProduceUniqueDatabaseName<TDataContext>();

            // build an ado.net connection string for the server and database configured.
            // turn off connection pooling to prevent it from holding on to the connection after we're done using it.
            context.ConnectionString = ProduceConnectionString(config, context.DbName, useConnectionPooling: false);

            var connectionString = context.ConnectionString;
            services.AddDbContext<TDataContext>(options => { options.UseSqlServer(connectionString); });


            // register the data migration runner.
            services.AddDataMigrationRunner();
        }

        protected override void InitializeSchemaAndData(IDbTestRunnerConfiguration config, IDbTestRunnerContext context , DbInitializationResult result)
        {
            if (!(config is DbTestRunnerConfiguration efConfig))
            {
                result.IsSuccessful = false;
                result.Logs.Add($"Failed to initialize schema and database.  Configuration is not of the expected type. Expected: {typeof(DbTestRunnerConfiguration).FullName} actual: {config.GetType().FullName}");
                return;
            }

            result.Logs.Add($"Preparing Database with {efConfig.DataContextType} migrations.");
            EnsureMigrations(efConfig, context, result);
            
        }

      
        /// <summary>
        /// Executes all the EF data context migrations against the configured database
        /// </summary>
        private void EnsureMigrations(DbTestRunnerConfiguration config, IDbTestRunnerContext context, DbInitializationResult result)
        {
            try
            {

                using (var scope = context.ServiceProvider.CreateScope())
                {
                    var migrationRunner = scope.ServiceProvider.GetService<DataMigrationRunner>();
                    migrationRunner.ApplyMigrations(config.DataContextType);
                    result.Logs.Add("DB Migrations completed.");
                    result.IsSuccessful = true;
                }
            }
            catch (Exception e)
            {
                result.Logs.Add($"Encountered exception running migrations. Exception: {e.Message} Details: {e}");
                throw;
            }

        }

        /// <summary>
        /// Produces a unique database name
        /// comprised on the data context name and current date time
        /// </summary>
        private string ProduceUniqueDatabaseName<TDataContext>()
        {
            var now = DateTime.Now;
            //var guidSuffix = Guid.NewGuid().ToString().Substring(24, 9);
            var ticksSuffix = DateTime.Now.Ticks;
            var timestamp = $"{now.Year}{now.Month:00}{now.Year}_{now.Hour:00}{now.Minute:00}{now.Second:00}";
            return $"DbTest_{typeof(TDataContext).Name}_{timestamp}_{ticksSuffix}";
        }
    }


}
