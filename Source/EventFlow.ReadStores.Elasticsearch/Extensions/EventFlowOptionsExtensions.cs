using System;
using Elasticsearch.Net.ConnectionPool;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static EventFlowOptions UseElasticsearchReadModel<TAggregate, TReadModel>(this EventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot
            where TReadModel : class, IEsReadModel, new()
        {
            eventFlowOptions.AddRegistration(new Registration<IReadModelStore<TAggregate>, EsReadModelStore<TAggregate, TReadModel>>());
            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticsearch(this EventFlowOptions eventFlowOptions, IConnectionSettingsValues connectionSettings)
        {
            eventFlowOptions.AddRegistration(new Registration<IElasticClient>(
                r => new ElasticClient(connectionSettings), Lifetime.Singleton));
            
            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticsearch(this EventFlowOptions eventFlowOptions, params Uri[] uris)
        {
            var connectionSettings = new ConnectionSettings(new StaticConnectionPool(uris))
                .ThrowOnElasticsearchServerExceptions();

            eventFlowOptions.ConfigureElasticsearch(connectionSettings);

            return eventFlowOptions;
        }
    }
}