using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.MsSql
{
    [Category(Categories.Integration)]
    public class EfMsSqlSnapshotTests : TestSuiteForSnapshotStore
    {
        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-snapshots");

            return eventFlowOptions
                .ConfigureEntityFramework(EntityFrameworkConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .ConfigureForSnapshotStoreTest<MsSqlDbContextProvider>()
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}
