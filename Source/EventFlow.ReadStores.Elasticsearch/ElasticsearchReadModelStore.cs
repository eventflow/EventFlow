// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
// https://github.com/rasmus/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
// 
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using EventFlow.Aggregates;
using EventFlow.Extensions;
using EventFlow.Logs;
using Nest;

namespace EventFlow.ReadStores.Elasticsearch
{
    public class ElasticsearchReadModelStore<TReadModel> :
        IElasticsearchReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        private readonly ILog _log;
        private readonly IElasticClient _elasticClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public ElasticsearchReadModelStore(
            ILog log,
            IElasticClient elasticClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
        {
            _log = log;
            _elasticClient = elasticClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() => $"Fetching read model '{typeof(TReadModel).PrettyPrint()}' with ID '{id}' from index '{readModelDescription.IndexName}'");

            var getResponse = await _elasticClient.GetAsync<TReadModel>(
                id,
                readModelDescription.IndexName.Value)
                .ConfigureAwait(false);

            if (!getResponse.IsValid || !getResponse.Found)
            {
                return ReadModelEnvelope<TReadModel>.Empty(id);
            }

            var version = long.Parse(getResponse.Version);
            return ReadModelEnvelope<TReadModel>.With(id, getResponse.Source, version);
        }

        public Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Information($"Deleting ALL '{typeof(TReadModel).PrettyPrint()}' read models from index '{readModelDescription.IndexName}'");

            return _elasticClient.DeleteByQueryAsync<TReadModel>(d => d
                .Index(readModelDescription.IndexName.Value)
                .Type<TReadModel>()
                .Query(q => q.MatchAll()));
        }

        public async Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() =>
                {
                    var readModelIds = readModelUpdates.Select(u => u.ReadModelId).Distinct().OrderBy(i => i).ToList();
                    return $"Updating read models of type '{typeof(TReadModel).PrettyPrint()}' with IDs '{string.Join(", ", readModelIds)}' in index '{readModelDescription.IndexName}'";
                });

            foreach (var readModelUpdate in readModelUpdates)
            {
                var response = await _elasticClient.GetAsync<TReadModel>(
                    readModelUpdate.ReadModelId,
                    readModelDescription.IndexName.Value)
                    .ConfigureAwait(false);

                var readModelEnvelope = response.Found
                    ? ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, response.Source, long.Parse(response.Version))
                    : ReadModelEnvelope<TReadModel>.Empty(readModelUpdate.ReadModelId);

                readModelEnvelope = await updateReadModel(readModelContext, readModelUpdate.DomainEvents, readModelEnvelope, cancellationToken).ConfigureAwait(false);

                await _elasticClient.IndexAsync(
                    readModelEnvelope.ReadModel,
                    d => d
                        .Id(readModelUpdate.ReadModelId)
                        .Index(readModelDescription.IndexName.Value)
                        .Version(readModelEnvelope.Version.GetValueOrDefault())
                        .VersionType(VersionType.ExternalGte))
                    .ConfigureAwait(false);
            }
        }
    }
}