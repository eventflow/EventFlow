using EventFlow.Aggregates;

namespace EventFlow.ReadStores.ElasticSearch
{
    public interface IEsReadModelStore<TAggregate, TReadModel> : IReadModelStore<TAggregate>
        where TReadModel : IEsReadModel, new()
        where TAggregate : IAggregateRoot
    {
    }
}