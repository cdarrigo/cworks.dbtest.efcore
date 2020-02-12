using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace cworks.DbTest.EFCore
{
    public static class DbTestContextExtensions
    {
        public static TDataContext ProduceDataContext<TDataContext>(this IDbTestRunnerContext context) where TDataContext : DbContext
        {
            return (TDataContext) ProduceDbContext(context, typeof(TDataContext));
        }

        public static DbContext ProduceDbContext(this IDbTestRunnerContext context, Type dataContextType)
        {
            return context.ServiceProvider.CreateScope().ServiceProvider.GetService(dataContextType) as DbContext;
        }
    }
}