using System;
using EventFlow.Configuration;
using EventFlow.EntityFramework.Extensions;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.MsSql
{
    [Category(Categories.Integration)]
    public class EfMsSqlReadStoreTests : TestSuiteForReadModelStore
    {
        private IMsSqlDatabase _testDatabase;

        protected override Type ReadModelType => typeof(ThingyReadModelEntity);

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            return eventFlowOptions
                .ConfigureEntityFramework(EntityFrameworkConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .ConfigureForReadStoreTest<MsSqlDbContextProvider>()
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}
