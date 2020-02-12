using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Linq;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Xunit;
using Xunit.Abstractions;

namespace cworks.DbTest.EFCore
{
    public abstract partial class DbTestSetup<TDataContext> where TDataContext : DbContext
    {
        [Collection(DbTestConstants.CollectionName)]
        public abstract class ContractDbTestBase : DbTest
        {

            protected ContractDbTestBase(ITestOutputHelper outputHelper, DbTestFixture testFixture) : base(outputHelper, testFixture)
            {
            }

            protected override SqlRequest Arrange(IDbTestRunnerContext context, TDataContext dataContext, ITestOutputHelper testOutputHelper)
            {
                return this.DoNothing();
            }

            protected override void AssertState(DataTable data, TDataContext dataContext, DataTable[] allData, ITestOutputHelper testOutputHelper)
            {

                Assert.True(data != null, "Failed to execute test against database object. Data Table is null");


                var actualColumnNames = data.Columns.Cast<DataColumn>().Select(i => i.ColumnName).ToArray();
                var hasAllExpectedColumns = true;
                foreach (var expected in this.ExpectedReturnColumnNames)
                {
                    if (!actualColumnNames.Contains(expected, StringComparer.InvariantCultureIgnoreCase))
                    {
                        this.TestOutputHelper.WriteLine($"{DbObjectType} {this.DbObjectName} failed to return expected column named {expected}");
                        hasAllExpectedColumns = false;
                    }
                }

                foreach (var actual in actualColumnNames)
                {
                    if (!ExpectedReturnColumnNames.Contains(actual, StringComparer.InvariantCultureIgnoreCase))
                    {
                        this.TestOutputHelper.WriteLine($"{DbObjectType} {this.DbObjectName} returned an unexpected column named {actual}");
                        hasAllExpectedColumns = false;
                    }
                }

                Assert.True(hasAllExpectedColumns);
            }

            protected abstract string DbObjectType { get; }
            protected abstract string DbObjectName { get; }
            protected virtual string SchemaName => "dbo";

            protected abstract string[] ExpectedReturnColumnNames { get; }

            protected string[] GetPropertyNamesFrom<T>()
            {
                return typeof(T).GetProperties()
                    .Where(i => i.GetCustomAttribute<NotMappedAttribute>() == null
                                && !IsComplexOrCustomTypeProperty(i))
                    .Select(i => i.Name)
                    .ToArray();
            }

            private bool IsComplexOrCustomTypeProperty(PropertyInfo pi)
            {
                if (pi.PropertyType.IsEnum) return false;
                if (pi.PropertyType.IsNullableType()) return false;
                if (pi.PropertyType.IsGenericType) return true;
                if (!pi.PropertyType.Namespace?.StartsWith("System") ?? false) return true;
                if (pi.PropertyType.IsArray) return true;
                if (pi.PropertyType.IsAbstract) return true;
                if (pi.PropertyType.IsInterface) return true;
                return false;
            }

        }
    }
}
