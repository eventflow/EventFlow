using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch
{
    public class EsReadModelStore<TAggregate, TReadModel> : ReadModelStore<TAggregate, TReadModel>, 
                                                            IEsReadModelStore<TAggregate, TReadModel> 
        where TReadModel : class, IEsReadModel, new() 
        where TAggregate : IAggregateRoot
    {
        private readonly IElasticClient _elasticClient;

        public EsReadModelStore(IElasticClient elasticClient, ILog log) : base(log)
        {
            _elasticClient = elasticClient;
        }

       public override async Task UpdateReadModelAsync(
            string aggregateId,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
       {
           var readModelResponse = await _elasticClient.GetAsync<TReadModel>(aggregateId)
               .ConfigureAwait(false);

           var readModel = readModelResponse.Source ??
                           new TReadModel
                           {
                               AggregateId = aggregateId,
                               CreateTime = DateTimeOffset.Now,
                               UpdatedTime = DateTimeOffset.Now
                           };

           ApplyEvents(readModel, domainEvents);

           Log.Debug("Indexing readmodel into elasticsearch: {0}", readModel);

           await _elasticClient.IndexAsync(readModel)
               .ConfigureAwait(false);
       }
    }
}