using System;
using System.Data;
using System.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace cworks.DbTest.EFCore
{
    public abstract partial class DbTestSetup<TDataContext> where TDataContext : DbContext
    {
        /// <summary>
        /// EF DbTest base class
        /// </summary>
        public abstract class DbTest : DbTestBase
        {
            protected DbTest(ITestOutputHelper outputHelper, DbTestFixture testFixture) : base(outputHelper, testFixture)
            {
                
            }

            /// <summary>
            /// (hook)
            /// Return the sql text to execute as part of the ARRANGE phase of the test.
            /// Or you may modify the data context directly.
            /// </summary>
            /// <returns>the sql text to execute</returns>

            protected abstract SqlRequest Arrange(IDbTestRunnerContext context, TDataContext dataContext, ITestOutputHelper testOutputHelper);



            /// <summary>
            /// (hook)
            /// Return the sql text to execute as part of the ACT phase of the test.
            /// Or you may modify the data context directly.
            /// </summary>
            /// <returns>the sql text to execute</returns>
            protected abstract SqlRequest Act(IDbTestRunnerContext context, TDataContext dataContext, ITestOutputHelper testOutputHelper);

            /// <summary>
            /// (hook)
            /// Write your assert statements here.
            /// You can reference the entities in the data context, or examine the data tables populated from the ACT method.
            /// </summary>
            /// <param name="data">First data table read</param>
            /// <param name="dataContext">EF Data Context</param>
            /// <param name="allData">All the data table read</param>
            /// <param name="testOutputHelper">Test Output Helper. Use this to write messages to the test output.</param>
            protected abstract void AssertState(DataTable data, TDataContext dataContext, DataTable[] allData, ITestOutputHelper testOutputHelper);


            protected override SqlRequest OnArrange(IDbTestRunnerContext context, IDisposable dbHandle, ITestOutputHelper testOutputHelper)
            {
                return Arrange(context, ToDataContext(dbHandle), testOutputHelper);
            }

            protected override SqlRequest OnAct(IDbTestRunnerContext context, IDisposable dbHandle, ITestOutputHelper testOutputHelper)
            {
                return Act(context, ToDataContext(dbHandle), testOutputHelper);
            }

            protected override void OnAssert(DataTable data, IDisposable dbHandle, DataTable[] allData, ITestOutputHelper testOutputHelper)
            {
                this.AssertState(data,ToDataContext(dbHandle),allData, this.TestOutputHelper);
            }

            private TDataContext ToDataContext(IDisposable dbHandle)
            {
                return (TDataContext) dbHandle;
            }
           
            
            private DbContext dbContext;
            

            protected override int CommitChanges()
            {
                return dbContext?.SaveChanges() ?? 0;
            }

            protected override SqlConnection GetDbConnection()
            {
                return (SqlConnection) dbContext?.Database.GetDbConnection();
            }

            protected override IDisposable ProduceDbHandle()
            {
                this.dbContext = this.TestFixture.Context.ProduceDataContext<TDataContext>();
                return dbContext;
            }
        }

    }
}
