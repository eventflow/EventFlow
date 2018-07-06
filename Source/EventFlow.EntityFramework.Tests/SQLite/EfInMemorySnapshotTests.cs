using EventFlow.Configuration;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    [Category(Categories.Integration)]
    public class EfSqliteSnapshotTests : TestSuiteForSnapshotStore
    {
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .ConfigureForSnapshotStoreTest<SqliteDbContextProvider>()
                .CreateResolver();
        }
    }
}
