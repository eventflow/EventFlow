// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using Elastic.Clients.Elasticsearch;
using Elastic.Transport;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Elasticsearch.ValueObjects;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Microsoft.Extensions.Logging;

namespace EventFlow.Elasticsearch.ReadStores
{
    public class ElasticsearchReadModelStore<TReadModel> :
        IElasticsearchReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        private readonly ElasticsearchClient _elasticClient;
        private readonly ILogger<ElasticsearchReadModelStore<TReadModel>> _logger;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public ElasticsearchReadModelStore(
            ILogger<ElasticsearchReadModelStore<TReadModel>> logger,
            IReadModelDescriptionProvider readModelDescriptionProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler,
            ElasticsearchClient elasticClient)
        {
            _logger = logger;
            _readModelDescriptionProvider = readModelDescriptionProvider;
            _transientFaultHandler = transientFaultHandler;
            _elasticClient = elasticClient;
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _logger.LogTrace("Fetching read model '{type}' with ID '{id}' from index '{IndexName}'",
                typeof(TReadModel).PrettyPrint(), id, readModelDescription.IndexName);

            var getResponse = await _elasticClient.GetAsync<TReadModel>(
                    id,
                    d => d
                        .RequestConfiguration(c => c
                            .AllowedStatusCodes((int) HttpStatusCode.NotFound))
                        .Index(readModelDescription.IndexName),
                    cancellationToken)
                .ConfigureAwait(false);

            if (!getResponse.IsValidResponse || !getResponse.Found) return ReadModelEnvelope<TReadModel>.Empty(id);

            return ReadModelEnvelope<TReadModel>.With(
                id,
                getResponse.Source!,
                getResponse.Version);
        }

        public async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            await _elasticClient.DeleteAsync<TReadModel>(
                    id,
                    d => d
                        .Index(readModelDescription.IndexName)
                        .RequestConfiguration(c => c
                            .AllowedStatusCodes((int) HttpStatusCode.NotFound)),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _logger.LogInformation("Deleting ALL '{Type}' by DELETING INDEX '{Index}'!",
                typeof(TReadModel).PrettyPrint(), readModelDescription.IndexName);


            await _elasticClient.Indices.DeleteAliasAsync(
                    readModelDescription.IndexName,
                    "_all",
                    d => d.RequestConfiguration(c => c.AllowedStatusCodes((int) HttpStatusCode.NotFound)),
                    cancellationToken
                )
                .ConfigureAwait(false);

            await _elasticClient.Indices.DeleteAsync(
                    readModelDescription.IndexName,
                    d => d.RequestConfiguration(c => c.AllowedStatusCodes((int) HttpStatusCode.NotFound)),
                    cancellationToken
                )
                .ConfigureAwait(false);
        }

        public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            foreach (var readModelUpdate in readModelUpdates)
                await _transientFaultHandler.TryAsync(
                        c => UpdateReadModelAsync(readModelDescription, readModelUpdate, readModelContextFactory,
                            updateReadModel, c),
                        Label.Named("elasticsearch-read-model-update"),
                        cancellationToken)
                    .ConfigureAwait(false);
        }

        private async Task UpdateReadModelAsync(
            ReadModelDescription readModelDescription,
            ReadModelUpdate readModelUpdate,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelId = readModelUpdate.ReadModelId;

            var response = await _elasticClient.GetAsync<TReadModel>(
                    readModelId,
                    d => d
                        .RequestConfiguration(c => c
                            .AllowedStatusCodes((int) HttpStatusCode.NotFound))
                        .Index(readModelDescription.IndexName),
                    cancellationToken)
                .ConfigureAwait(false);

            var isNew = !response.Found;

            var readModelEnvelope = isNew
                ? ReadModelEnvelope<TReadModel>.Empty(readModelId)
                : ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, response.Source!, response.Version);

            var context = readModelContextFactory.Create(readModelId, isNew);

            var readModelUpdateResult = await updateReadModel(
                    context,
                    readModelUpdate.DomainEvents,
                    readModelEnvelope,
                    cancellationToken)
                .ConfigureAwait(false);
            if (!readModelUpdateResult.IsModified) return;

            readModelEnvelope = readModelUpdateResult.Envelope;
            if (context.IsMarkedForDeletion)
            {
                await DeleteAsync(readModelId, cancellationToken).ConfigureAwait(false);
                return;
            }

            try
            {
                var resp =
                    await _elasticClient.IndexAsync(
                            readModelEnvelope.ReadModel,
                            d =>
                            {
                                d = d
                                    .RequestConfiguration(c => c)
                                    .Id(readModelId)
                                    .Index(readModelDescription.IndexName);
                                d = isNew
                                    ? d.OpType(OpType.Create)
                                    : d.VersionType(VersionType.ExternalGte)
                                        .Version(readModelEnvelope.Version.GetValueOrDefault());
                            },
                            cancellationToken)
                        .ConfigureAwait(false);


            }
            catch (TransportException ex) when(ex is  {ApiCallDetails: {HttpStatusCode: 409}})
            {
                 throw new OptimisticConcurrencyException(
                    $"Read model '{readModelId}' updated by another", ex);
            }



        }
    }
}