using System;
using System.Threading;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.EventStores
{
    [Category(Categories.Integration)]
    public class MsSqlEventStoreWithCustomSchemaTests : TestSuiteForEventStore
    {
        private const string CustomSchema = "eventflow";
        private IMsSqlDatabase _testDatabase;

        protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-custom-schema");
            _testDatabase.Execute($"CREATE SCHEMA [{CustomSchema}]");

            eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New
                    .SetConnectionString(_testDatabase.ConnectionString.Value)
                    .SetSchema(new Schema(CustomSchema)))
                .UseMssqlEventStore();

            var serviceProvider = base.Configure(eventFlowOptions);

            var databaseMigrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabaseAsync(databaseMigrator, CancellationToken.None).Wait();
            databaseMigrator.MigrateDatabaseUsingEmbeddedScriptsAsync(
                GetType().Assembly,
                null, /* TODO */
                CancellationToken.None).Wait();

            return serviceProvider;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.Dispose();
        }
    }
}
