using System;
using Elasticsearch.Net.ConnectionPool;
using EventFlow.Aggregates;
using EventFlow.Configuration.Registrations;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static EventFlowOptions UseElasticsearchReadModel<TAggregate, TReadModel>(this EventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot
            where TReadModel : class, IEsReadModel, new()
        {
            eventFlowOptions.RegisterServices(sr => sr.Register<IReadModelStore<TAggregate>, EsReadModelStore<TAggregate, TReadModel>>());
            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticsearch(this EventFlowOptions eventFlowOptions, IConnectionSettingsValues connectionSettings)
        {
            eventFlowOptions.RegisterServices(sr => sr.Register<IElasticClient>(f => new ElasticClient(connectionSettings), Lifetime.Singleton));
            
            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticsearch(this EventFlowOptions eventFlowOptions, params Uri[] uris)
        {
            var connectionSettings = new ConnectionSettings(new StaticConnectionPool(uris), "eventflow")
                .ThrowOnElasticsearchServerExceptions();

            eventFlowOptions.ConfigureElasticsearch(connectionSettings);

            return eventFlowOptions;
        }
    }
}