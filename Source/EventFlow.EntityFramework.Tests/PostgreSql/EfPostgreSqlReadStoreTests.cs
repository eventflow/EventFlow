using System;
using EventFlow.Configuration;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.Extensions;
using EventFlow.PostgreSql.Connections;
using EventFlow.PostgreSql.Extensions;
using EventFlow.PostgreSql.TestsHelpers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.EntityFramework.Tests.PostgreSql
{
    [Category(Categories.Integration)]
    public class EfPostgreSqlReadStoreTests : TestSuiteForReadModelStore
    {
        private IPostgreSqlDatabase _testDatabase;

        protected override Type ReadModelType => typeof(ThingyReadModelEntity);

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = PostgreSqlHelpz.CreateDatabase("eventflow");

            return eventFlowOptions
                .ConfigurePostgreSql(PostgreSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .ConfigureForReadStoreTest<PostgreSqlDbContextProvider>()
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("Failed to delete database");
        }
    }
}
