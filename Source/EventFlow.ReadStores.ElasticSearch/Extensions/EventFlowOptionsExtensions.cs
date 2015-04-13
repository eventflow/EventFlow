using System;
using Elasticsearch.Net;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using Nest;

namespace EventFlow.ReadStores.ElasticSearch.Extensions
{
    public static class EventFlowOptionsExtensions
    {
        public static EventFlowOptions UseElasticSearchReadModel<TAggregate, TReadModel>(this EventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot
            where TReadModel : IEsReadModel, new()
        {
            eventFlowOptions.AddRegistration(new Registration<IReadModelStore<TAggregate>, EsReadModelStore<TAggregate, TReadModel>>());
            return eventFlowOptions;
        }

        public static EventFlowOptions ConfigureElasticSearch(this EventFlowOptions eventFlowOptions, IEsConfiguration esConfiguration)
        {
            eventFlowOptions.AddRegistration(
                new Registration<IElasticsearchClient>(
                    r => new ElasticsearchClient(new ConnectionSettings(new Uri(esConfiguration.ConnectionString)))));
            eventFlowOptions.AddRegistration(new Registration<IEsConfiguration>(r => esConfiguration, Lifetime.Singleton));
            
            return eventFlowOptions;
        }
    }
}