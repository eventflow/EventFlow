using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores.MsSql;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.MsSql.Tests.ReadModels;
using EventFlow.ReadStores.MsSql;
using EventFlow.ReadStores.MsSql.Extensions;
using EventFlow.Test;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.ReadModels;
using TestAggregateReadModel = EventFlow.MsSql.Tests.ReadModels.TestAggregateReadModel;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public class MsSqlIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected ITestDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }
        protected IReadModelSqlGenerator ReadModelSqlGenerator { get; private set; }

        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(TestDatabase.ConnectionString))
                .UseEventStore<MsSqlEventStore>()
                .UseMssqlReadModel<TestAggregate, TestAggregateReadModel>()
                .CreateResolver();

            MsSqlConnection = resolver.Resolve<IMsSqlConnection>();
            ReadModelSqlGenerator = resolver.Resolve<IReadModelSqlGenerator>();

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModel(string id)
        {
            var sql = ReadModelSqlGenerator.CreateSelectSql<TestAggregateReadModel>();
            var readModels = await MsSqlConnection.QueryAsync<TestAggregateReadModel>(
                CancellationToken.None,
                sql,
                new {AggregateId = id})
                .ConfigureAwait(false);
            return readModels.SingleOrDefault();
        }

        public override void TearDown()
        {
            TestDatabase.Dispose();
        }
    }
}
