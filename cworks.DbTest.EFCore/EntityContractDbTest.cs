using System.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace cworks.DbTest.EFCore
{
    public abstract partial class DbTestSetup<TDataContext> where TDataContext : DbContext
    {

            [Collection(DbTestConstants.CollectionName)]
            public class EntityContractDbTest<TEntity> : ContractDbTestBase
            { 
                protected readonly string TableOrViewName;
                protected readonly string Schema;

                public EntityContractDbTest(ITestOutputHelper outputHelper, DbTestFixture testFixture, string tableOrViewName = null, string schema = null) : base(outputHelper, testFixture)
                {
                    this.TableOrViewName = tableOrViewName;
                    this.Schema = schema;
                }

                protected override SqlRequest Act(IDbTestRunnerContext context, TDataContext dataContext, ITestOutputHelper testOutputHelper)
                {
                    // return just an empty data set so we can examine the shape of the return object.
                    return SqlRequest.ReturnNoRows(this.GetQualifiedTableOrViewName<TEntity>(this.TableOrViewName, this.Schema));

                }

                protected override void AssertState(DataTable data, TDataContext dataContext, DataTable[] allData, ITestOutputHelper testOutputHelper)
                {
                    // You can use some helper methods to perform some quality assertions 
                    // on the data table, ensuring the view contract still matches the entity type
                    // used in the DbQuery<TEntity> or DbSet<TEntity>. 
                    data.AssertHasModelProperties<TEntity>(testOutputHelper);
                }

            
                protected override string DbObjectName => typeof(TEntity).Name;
                protected override string DbObjectType => "Table/View";
                protected override string[] ExpectedReturnColumnNames => GetPropertyNamesFrom<TEntity>();
            }

        }
    }
