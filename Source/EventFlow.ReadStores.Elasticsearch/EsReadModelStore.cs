using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
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
        private readonly string[] _indicesToFeed;

        public EsReadModelStore(IElasticClient elasticClient, ILog log, string[] indicesToFeed) : base(log)
        {
            _elasticClient = elasticClient;
            _indicesToFeed = indicesToFeed;
        }

        public override async Task UpdateReadModelAsync(
            string aggregateId,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            var feedTasks = _indicesToFeed.Select(i => FeedReadModelToIndex(aggregateId, domainEvents, i));
            await Task.WhenAll(feedTasks);
        }

        private async Task FeedReadModelToIndex(string aggregateId, IReadOnlyCollection<IDomainEvent> domainEvents, string index)
        {
            var readModelResponse = await _elasticClient.GetAsync<TReadModel>(aggregateId, index)
                .ConfigureAwait(false);

            var readModel = readModelResponse.Source ??
                            new TReadModel
                            {
                                AggregateId = aggregateId,
                                CreateTime = DateTimeOffset.Now,
                                UpdatedTime = DateTimeOffset.Now
                            };

            ApplyEvents(readModel, domainEvents);

            Log.Debug("Indexing readmodel into index '{0}': {1}", index, readModel);

            await
                _elasticClient.IndexAsync(readModel,
                    i => i.Index(index)
                        .Version(readModel.LastAggregateSequenceNumber)
                        .VersionType(VersionType.External))
                    .ConfigureAwait(false);
        }
    }
}