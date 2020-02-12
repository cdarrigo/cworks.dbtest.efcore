using System;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace cworks.DbTest.EFCore
{
    
    public abstract partial class DbTestSetup<TDataContext>:DbTestSetup, IRegisterDataContext
        where TDataContext : DbContext
    {

        //
        // EFCORE
        //


        protected override IDbTestRunnerConfiguration ProduceDefaultConfiguration(IConfiguration systemConfig)
        {
            var config = DbTestRunnerConfiguration.ProduceDefaultConfiguration<TDataContext>(systemConfig);
            return config;
        }

        protected override void AfterDbTestsConfigured(IDbTestRunnerConfiguration config)
        {
            if (config is DbTestRunnerConfiguration efConfig)
            {
                if (efConfig.DataContextType == null)
                {
                    efConfig.DataContextType = typeof(TDataContext);
                }
            }
        }

        public void RegisterDataContext(IServiceCollection services, IDbTestRunnerContext context, DbTestRunnerConfiguration config, IEfDbScaffolder scaffolder)
        {
            scaffolder.RegisterServices<TDataContext>(services, context, config);
        }

        protected override void RegisterServices(IServiceCollection services, IDbTestRunnerContext context, IDbTestRunnerConfiguration config, IDbScaffolder scaffolder)
        {
            var efScaffolder = scaffolder as EfDbScaffolder;
            var efConfig = config as DbTestRunnerConfiguration;

            efScaffolder?.RegisterServices<TDataContext>(services,context, efConfig);
        }

        protected override IDbScaffolder ProduceScaffolder()
        {
            return new EfDbScaffolder();
        }

        public override void TearDownDatabase(IDbTestRunnerContext context, IDbTestRunnerConfiguration config, bool allTestsWereSuccessful)
        {
            if (!(config is DbTestRunnerConfiguration efConfig)) return;

            // clears the connection pool for the specified connection 
            // this lets us drop the database without having hanging active connections
            using (var dataContext = context.ProduceDbContext(efConfig.DataContextType))
            {

                if (dataContext.Database.GetDbConnection() is SqlConnection sqlConnection)
                {
                    SqlConnection.ClearPool(sqlConnection);
                }
            }

            if ((allTestsWereSuccessful && config.DropDatabaseOnSuccess) ||
                (!allTestsWereSuccessful && config.DropDatabaseOnFailure))
            {
                context.DbScaffolder.DropDatabase(context, config);
            }
        }

        /// <summary>
        /// Produces a unique database name
        /// comprised on the data context name and current date time
        /// </summary>
        protected override string ProduceUniqueDatabaseName()
        {
            var now = DateTime.Now;
            //var guidSuffix = Guid.NewGuid().ToString().Substring(24, 9);
            var ticksSuffix = DateTime.Now.Ticks;
            var timestamp = $"{now.Year}{now.Month:00}{now.Year}_{now.Hour:00}{now.Minute:00}{now.Second:00}";
            return $"DbTest_{typeof(TDataContext).Name}_{timestamp}_{ticksSuffix}";
        }
        
    }
}