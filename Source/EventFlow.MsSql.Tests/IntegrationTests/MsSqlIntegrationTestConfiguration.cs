using EventFlow.Configuration;
using EventFlow.EventStores.MsSql;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.Test;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public class MsSqlIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected ITestDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }

        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(TestDatabase.ConnectionString))
                .UseEventStore<MsSqlEventStore>()
                .CreateResolver();

            MsSqlConnection = resolver.Resolve<IMsSqlConnection>();
            EventFlowEventStoresMsSql.MigrateDatabase(resolver.Resolve<IMsSqlDatabaseMigrator>());

            return resolver;
        }

        public override void TearDown()
        {
            TestDatabase.Dispose();
        }
    }
}
