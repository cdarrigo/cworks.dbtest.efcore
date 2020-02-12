using System;
using Microsoft.Extensions.Configuration;

namespace cworks.DbTest.EFCore
{
    public class DbTestRunnerConfiguration:DbTestRunnerConfigurationBase 
    {
        public DbTestRunnerConfiguration(string server) : base(server)
        {
        }

        public DbTestRunnerConfiguration(string server, string username, string password) : base(server, username, password)
        {
        }

        public DbTestRunnerConfiguration()
        {
         
        }

        public override void Validate()
        {
            base.Validate();
            if (this.DataContextType == null) throw new DbTestSetupException("DataContextType is required.");
        }

        public Type DataContextType { get; set; }

        public static DbTestRunnerConfiguration ProduceDefaultConfiguration<TDataContext>(IConfiguration systemConfig)
        {
            var config= ProduceTestConfiguration<DbTestRunnerConfiguration>(systemConfig);
            config.DataContextType = typeof(TDataContext);
            return config;
        }

      
    }
    /*
     "DbTest": {
    "Connect": {
      "ServerName" : ".",
      "UserName" : "",
      "Password" : "",
      "UseIntegratedSecurity": true
    },
    "DbName":"",
    "DropDatabaseOnFailure":"true",
    "DropDatabaseOnSuccess": "true",
    "Enabled": "true"

  }
     */

   
}
