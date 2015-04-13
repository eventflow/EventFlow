using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores.MsSql;
using EventFlow.MsSql;
using EventFlow.MsSql.Extensions;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.ReadStores.ElasticSearch.Extensions;
using EventFlow.Test;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.ReadModels;
using TestAggregateReadModel = EventFlow.ReadStores.ElasticSearch.Tests.ReadModels.TestAggregateReadModel;

namespace EventFlow.ReadStores.ElasticSearch.Tests.IntegrationsTests
{
    public class EsIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected ITestDatabase TestDatabase { get; private set; }
        protected IMsSqlConnection MsSqlConnection { get; private set; }
        
        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            TestDatabase = MsSqlHelper.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(TestDatabase.ConnectionString))
                .ConfigureElasticSearch(EsConfiguration.New.SetConnectionString("http://127.0.0.1:9200"))
                .UseEventStore<MsSqlEventStore>()
                .UseElasticSearchReadModel<TestAggregate, TestAggregateReadModel>()
                .CreateResolver();

            MsSqlConnection = resolver.Resolve<IMsSqlConnection>();
            
            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModel(string id)
        {
            //var sql = ReadModelSqlGenerator.CreateSelectSql<TestAggregateReadModel>();
            //var readModels = await MsSqlConnection.QueryAsync<TestAggregateReadModel>(
            //    CancellationToken.None,
            //    sql,
            //    new {AggregateId = id})
            //    .ConfigureAwait(false);
            //return readModels.SingleOrDefault();

            return await Task.FromResult(new TestAggregateReadModel());
        }

        public override void TearDown()
        {
            TestDatabase.Dispose();
        }
    }
}
