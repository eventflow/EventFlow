using System;
using System.Threading;
using EventFlow.Extensions;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.MsSql.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    public class MsSqlReadModelStoreWithCustomSchemaTests : TestSuiteForReadModelStore
    {
        private const string CustomSchema = "eventflow";

        protected override Type ReadModelType { get; } = typeof(MsSqlThingyReadModel);

        private IMsSqlDatabase _testDatabase;

        protected override IServiceProvider Configure(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-readmodels-custom-schema");
            _testDatabase.Execute($"CREATE SCHEMA [{CustomSchema}]");

            eventFlowOptions
                .RegisterServices(sr => sr.AddTransient(typeof(ThingyMessageLocator)))
                .ConfigureMsSql(MsSqlConfiguration.New
                    .SetConnectionString(_testDatabase.ConnectionString.Value)
                    .SetSchema(new Schema(CustomSchema)))
                .UseMssqlReadModel<MsSqlThingyReadModel>()
                .UseMssqlReadModel<MsSqlThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(MsSqlThingyGetQueryHandler),
                    typeof(MsSqlThingyGetVersionQueryHandler),
                    typeof(MsSqlThingyGetMessagesQueryHandler));

            var serviceProvider = base.Configure(eventFlowOptions);

            var databaseMigrator = serviceProvider.GetRequiredService<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabaseAsync(databaseMigrator, CancellationToken.None).Wait();
            databaseMigrator.MigrateDatabaseUsingEmbeddedScriptsAsync(
                GetType().Assembly,
                null /* TODO */,
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