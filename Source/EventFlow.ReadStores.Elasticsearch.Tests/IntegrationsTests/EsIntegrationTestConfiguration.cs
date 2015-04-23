using System;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.ReadStores.Elasticsearch.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;
using Nest;
using TestAggregateReadModel = EventFlow.ReadStores.Elasticsearch.Tests.ReadModels.TestAggregateReadModel;

namespace EventFlow.ReadStores.Elasticsearch.Tests.IntegrationsTests
{
    public class EsIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        private string _indexToFeed;
        protected IElasticClient ElasticClient { get; private set; }
        
        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            var envUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");

            var elasticsearchUrl = string.IsNullOrEmpty(envUri)
                ? "http://127.0.0.1:9200"
                : envUri;

            _indexToFeed = string.Format("testindex-{0}", Guid.NewGuid());

            var resolver = eventFlowOptions
                .ConfigureElasticsearch(new Uri(elasticsearchUrl))
                .UseElasticsearchReadModel<TestAggregate, TestAggregateReadModel>(_indexToFeed)
                .CreateResolver();

            ElasticClient = resolver.Resolve<IElasticClient>();
            
            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModel(string id)
        {
            var aggregateResponse = await ElasticClient.GetAsync<TestAggregateReadModel>(id, _indexToFeed)
                .ConfigureAwait(false);

            return aggregateResponse.Source;
        }

        public override void TearDown()
        {
            ElasticClient.DeleteIndex(d => d.Index(_indexToFeed));
        }
    }
}
