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
    public class EfMsSqlEventStoreTests : TestSuiteForEventStore
    {
        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            return eventFlowOptions
                .ConfigureEntityFramework(EntityFrameworkConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .ConfigureForEventStoreTest<MsSqlDbContextProvider>()
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}
