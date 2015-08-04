// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Elasticsearch.Net;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.ReadStores.Elasticsearch
{
    public class ElasticsearchReadModelStore<TReadModel> : ReadModelStore<TReadModel>,
        IElasticsearchReadModelStore<TReadModel>
        where TReadModel : class, IElasticsearchReadModel, new()
    {
        private readonly IElasticsearchClient _elasticsearchClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public ElasticsearchReadModelStore(
            ILog log,
            IElasticsearchClient elasticsearchClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
            : base(log)
        {
            _elasticsearchClient = elasticsearchClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();
            var elasticsearchResponse = await _elasticsearchClient.GetAsync<TReadModel>(
                readModelDescription.IndexName.Value,
                readModelDescription.TypeName.Value,
                id)
                .ConfigureAwait(false);

            return elasticsearchResponse.Response == null
                ? ReadModelEnvelope<TReadModel>.Empty
                : ReadModelEnvelope<TReadModel>.With(elasticsearchResponse.Response);
        }

        public override Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();
            return _elasticsearchClient.DeleteAsync(
                readModelDescription.IndexName.Value,
                readModelDescription.TypeName.Value,
                id);
        }

        public override Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
