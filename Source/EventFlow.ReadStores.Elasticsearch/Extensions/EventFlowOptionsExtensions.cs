using System;
using Elasticsearch.Net.ConnectionPool;
using EventFlow.Aggregates;
using EventFlow.Configuration.Registrations;
using EventFlow.Logs;
using EventFlow.ReadStores.Elasticsearch.Configuration;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static EventFlowOptions UseElasticsearchReadModel<TAggregate, TReadModel>(this EventFlowOptions eventFlowOptions, params string[] indicesToFeed)
            where TAggregate : IAggregateRoot
            where TReadModel : class, IEsReadModel, new()
        {
            eventFlowOptions.UseElasticsearchReadModel<TAggregate, TReadModel>(new SimpleIndexIdentifier(indicesToFeed));

            return eventFlowOptions;
        }

        public static EventFlowOptions UseElasticsearchReadModel<TAggregate, TReadModel>(
            this EventFlowOptions eventFlowOptions, params IIndexIdentifier[] indexIdentifiers)
            where TAggregate : IAggregateRoot
            where TReadModel : class, IEsReadModel, new()
        {
            eventFlowOptions.RegisterServices(sr =>
            {
                sr.Register<IReadModelStore<TAggregate>, EsReadModelStore<TAggregate, TReadModel>>();
                sr.Register<IIndexProvider>(f => new IndexProvider(indexIdentifiers, f.Resolver.Resolve<IElasticClient>()));
            });

            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticsearch(this EventFlowOptions eventFlowOptions, IConnectionSettingsValues connectionSettings)
        {
            eventFlowOptions.RegisterServices(sr => sr.Register<IElasticClient>(f => new ElasticClient(connectionSettings), Lifetime.Singleton));
            
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