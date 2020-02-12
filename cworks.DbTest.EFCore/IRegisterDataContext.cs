using Microsoft.Extensions.DependencyInjection;

namespace cworks.DbTest.EFCore
{
    public interface IRegisterDataContext
    {
        void RegisterDataContext(IServiceCollection services, IDbTestRunnerContext context, DbTestRunnerConfiguration config, IEfDbScaffolder scaffolder);
    }
}