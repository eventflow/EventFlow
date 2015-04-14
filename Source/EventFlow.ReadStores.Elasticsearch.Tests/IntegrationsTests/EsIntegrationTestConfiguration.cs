using System;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.ReadStores.Elasticsearch.Extensions;
using EventFlow.Test;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.ReadModels;
using Nest;
using TestAggregateReadModel = EventFlow.ReadStores.Elasticsearch.Tests.ReadModels.TestAggregateReadModel;

namespace EventFlow.ReadStores.Elasticsearch.Tests.IntegrationsTests
{
    public class EsIntegrationTestConfiguration : IntegrationTestConfiguration
    {
        protected IElasticClient ElasticClient { get; private set; }
        
        public override IRootResolver CreateRootResolver(EventFlowOptions eventFlowOptions)
        {
            var envUri = Environment.GetEnvironmentVariable("ELASTICSEARCH_URL");

            var elasticsearchUrl = string.IsNullOrEmpty(envUri)
                ? "http://127.0.0.1:9200"
                : envUri;

            var resolver = eventFlowOptions
                .ConfigureElasticsearch(new Uri(elasticsearchUrl))
                .UseElasticsearchReadModel<TestAggregate, TestAggregateReadModel>()
                .CreateResolver();

            ElasticClient = resolver.Resolve<IElasticClient>();
            
            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModel(string id)
        {
            var aggregateResponse = await ElasticClient.GetAsync<TestAggregateReadModel>(id)
                .ConfigureAwait(false);

            return aggregateResponse.Source;
        }

        public override void TearDown()
        {
        }
    }
}
