// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Elasticsearch.ValueObjects;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Nest;

namespace EventFlow.Elasticsearch.ReadStores
{
    public class ElasticsearchReadModelStore<TReadModel> :
        IElasticsearchReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        private readonly ILog _log;
        private readonly IElasticClient _elasticClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public ElasticsearchReadModelStore(
            ILog log,
            IElasticClient elasticClient,
            IReadModelDescriptionProvider readModelDescriptionProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
        {
            _log = log;
            _elasticClient = elasticClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
            _transientFaultHandler = transientFaultHandler;
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() => $"Fetching read model '{typeof(TReadModel).PrettyPrint()}' with ID '{id}' from index '{readModelDescription.IndexName}'");

            var getResponse = await _elasticClient.GetAsync<TReadModel>(
                id,
                d => d
                    .RequestConfiguration(c => c
                        .AllowedStatusCodes((int)HttpStatusCode.NotFound))
                        .Index(readModelDescription.IndexName.Value), 
                            cancellationToken)
                .ConfigureAwait(false);

            if (!getResponse.IsValid || !getResponse.Found)
            {
                return ReadModelEnvelope<TReadModel>.Empty(id);
            }

            return ReadModelEnvelope<TReadModel>.With(
                id,
                getResponse.Source,
                getResponse.Version);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            await _elasticClient.DeleteAsync(
                new DocumentPath<TReadModel>(id),
                d => d
                    .Index(readModelDescription.IndexName.Value)
                    .RequestConfiguration(c => c
                        .AllowedStatusCodes((int) HttpStatusCode.NotFound)),
                cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Information($"Deleting ALL '{typeof(TReadModel).PrettyPrint()}' by DELETING INDEX '{readModelDescription.IndexName}'!");

            var aliasResponse = await _elasticClient.GetAliasAsync(x => x.Name(readModelDescription.IndexName.Value), cancellationToken)
                .ConfigureAwait(false);

            if (aliasResponse.ApiCall.Success)
            {
                foreach (var indicesKey in aliasResponse.Indices.Keys)
                {
                    await _elasticClient.DeleteIndexAsync(indicesKey,
                            d => d.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)),
                            cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            foreach (var readModelUpdate in readModelUpdates)
            {
                await _transientFaultHandler.TryAsync(
                    c => UpdateReadModelAsync(readModelDescription, readModelUpdate, readModelContextFactory, updateReadModel, c),
                    Label.Named("elasticsearch-read-model-update"),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UpdateReadModelAsync(
            ReadModelDescription readModelDescription,
            ReadModelUpdate readModelUpdate,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelId = readModelUpdate.ReadModelId;

            var response = await _elasticClient.GetAsync<TReadModel>(
                readModelId,
                d => d
                    .RequestConfiguration(c => c
                        .AllowedStatusCodes((int)HttpStatusCode.NotFound))
                        .Index(readModelDescription.IndexName.Value), 
                            cancellationToken)
                .ConfigureAwait(false);

            var isNew = !response.Found;

            var readModelEnvelope = isNew
                ? ReadModelEnvelope<TReadModel>.Empty(readModelId)
                : ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, response.Source, response.Version);

            var context = readModelContextFactory.Create(readModelId, isNew);

            var readModelUpdateResult = await updateReadModel(
                context,
                readModelUpdate.DomainEvents,
                readModelEnvelope,
                cancellationToken)
                .ConfigureAwait(false);
            if (!readModelUpdateResult.IsModified)
            {
                return;
            }

            readModelEnvelope = readModelUpdateResult.Envelope;
            if (context.IsMarkedForDeletion)
            {
                await DeleteAsync(readModelId, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                await _elasticClient.IndexAsync(
                    readModelEnvelope.ReadModel,
                    d =>
                    {
                        d = d
                            .RequestConfiguration(c => c)
                            .Id(readModelId)
                            .Index(readModelDescription.IndexName.Value);
                        d = isNew
                            ? d.OpType(OpType.Create)
                            : d.VersionType(VersionType.ExternalGte).Version(readModelEnvelope.Version.GetValueOrDefault());
                        return d;
                    },
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (ElasticsearchClientException e)
                when (e.Response?.HttpStatusCode == (int)HttpStatusCode.Conflict)
            {
                throw new OptimisticConcurrencyException(
                    $"Read model '{readModelId}' updated by another",
                    e);
            }
        }
    }
}