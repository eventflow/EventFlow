using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;

namespace EventFlow.ReadStores.ElasticSearch
{
    public class EsReadModelStore<TAggregate, TReadModel> : IEsReadModelStore<TAggregate, TReadModel> 
        where TReadModel : IEsReadModel, new() 
        where TAggregate : IAggregateRoot
    {
        public Task UpdateReadModelAsync(string aggregateId, IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}