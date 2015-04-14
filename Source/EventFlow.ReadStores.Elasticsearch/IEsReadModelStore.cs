using EventFlow.Aggregates;

namespace EventFlow.ReadStores.Elasticsearch
{
    public interface IEsReadModelStore<TAggregate, TReadModel> : IReadModelStore<TAggregate>
        where TReadModel : IEsReadModel, new()
        where TAggregate : IAggregateRoot
    {
    }
}