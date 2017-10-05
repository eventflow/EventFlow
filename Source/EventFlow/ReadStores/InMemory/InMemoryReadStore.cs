// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.ReadStores.InMemory
{
    public class InMemoryReadStore<TReadModel> : ReadModelStore<TReadModel>, IInMemoryReadStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        private readonly Dictionary<string, ReadModelEnvelope<TReadModel>> _readModels = new Dictionary<string, ReadModelEnvelope<TReadModel>>();
        private readonly AsyncLock _asyncLock = new AsyncLock();

        public InMemoryReadStore(
            ILog log)
            : base(log)
        {
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                ReadModelEnvelope<TReadModel> readModelEnvelope;
                return _readModels.TryGetValue(id, out readModelEnvelope)
                    ? readModelEnvelope
                    : ReadModelEnvelope<TReadModel>.Empty(id);
            }
        }

        public async Task<IReadOnlyCollection<TReadModel>> FindAsync(
            Predicate<TReadModel> predicate,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                return _readModels.Values
                    .Where(e => predicate(e.ReadModel))
                    .Select(e => e.ReadModel)
                    .ToList();
            }
        }

        public override async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _readModels.Remove(id);
            }
        }

        public override async Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _readModels.Clear();
            }
        }

        public override async Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                foreach (var readModelUpdate in readModelUpdates)
                {
                    ReadModelEnvelope<TReadModel> readModelEnvelope;
                    if (!_readModels.TryGetValue(readModelUpdate.ReadModelId, out readModelEnvelope))
                    {
                        readModelEnvelope = ReadModelEnvelope<TReadModel>.Empty(readModelUpdate.ReadModelId);
                    }

                    readModelEnvelope = await updateReadModel(
                        readModelContext,
                        readModelUpdate.DomainEvents,
                        readModelEnvelope,
                        cancellationToken)
                        .ConfigureAwait(false);

                    _readModels[readModelUpdate.ReadModelId] = readModelEnvelope;
                }
            }
        }
    }
}