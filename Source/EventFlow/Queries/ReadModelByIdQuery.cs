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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.ReadStores;

namespace EventFlow.Queries
{
    public class ReadModelByIdQuery<TReadModel> : IQuery<TReadModel>
        where TReadModel : class, IReadModel
    {
        public string Id { get; }

        public ReadModelByIdQuery(IIdentity identity)
            : this(identity.Value)
        {
        }

        public ReadModelByIdQuery(string id)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentNullException(nameof(id));

            Id = id;
        }
    }

    public class ReadModelByIdQueryHandler<TReadStore, TReadModel> : IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>
        where TReadStore : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        private readonly TReadStore _readStore;

        public ReadModelByIdQueryHandler(
            TReadStore readStore)
        {
            _readStore = readStore;
        }

        public async Task<TReadModel> ExecuteQueryAsync(ReadModelByIdQuery<TReadModel> query, CancellationToken cancellationToken)
        {
            var readModelEnvelope = await _readStore.GetAsync(query.Id, cancellationToken).ConfigureAwait(false);
            return readModelEnvelope.ReadModel;
        }
    }
}