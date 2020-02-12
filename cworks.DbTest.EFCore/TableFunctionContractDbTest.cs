using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace cworks.DbTest.EFCore
{
    public abstract partial class DbTestSetup<TDataContext> where TDataContext : DbContext
    {

        [Collection(DbTestConstants.CollectionName)]
        public abstract class TableFunctionContractDbTest: ContractDbTestBase
        {
            protected TableFunctionContractDbTest(ITestOutputHelper outputHelper, DbTestFixture testFixture) : base(outputHelper, testFixture)
            {
            }

            protected override SqlRequest Act(IDbTestRunnerContext context, TDataContext dataContext, ITestOutputHelper testOutputHelper)
            {
                var functionParameters = GetParametersForDbObject(this.FunctionName, context );
                return this.InvokeTableFunction(this.FunctionName, this.SchemaName, functionParameters);
            }

            protected abstract string FunctionName { get; }
            protected override string DbObjectName => FunctionName;
            protected override string DbObjectType => "Function";

        }

    }
}
