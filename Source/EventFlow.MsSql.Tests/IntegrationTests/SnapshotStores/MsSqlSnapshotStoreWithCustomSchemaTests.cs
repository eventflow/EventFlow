using System;
using System.Threading;
using EventFlow.Extensions;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.SnapshotStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.SnapshotStores
{
    [Category(Categories.Integration)]
    public class MsSqlSnapshotStoreWithCustomSchemaTests : TestSuiteForSnapshotStore
    {
        private const string CustomSchema = "eventflow";

        private IMsSqlDatabase _testDatabase;

        protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-snapshots-custom-schema");
            _testDatabase.Execute($"CREATE SCHEMA [{CustomSchema}]");

            eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New
                    .SetConnectionString(_testDatabase.ConnectionString.Value)
                    .SetSchema(new Schema(CustomSchema)))
                .UseMsSqlSnapshotStore();

            var serviceProvider = base.Configure(eventFlowOptions);

            var databaseMigrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
            EventFlowSnapshotStoresMsSql.MigrateDatabaseAsync(
                databaseMigrator,
                CancellationToken.None).Wait();

            return serviceProvider;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe(Logger, "Failed to delete database");
        }
    }
}
