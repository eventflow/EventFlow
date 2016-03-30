// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Snapshots;

namespace EventFlow.Aggregates
{
    public class AggregateStore : IAggregateStore
    {
        private readonly IAggregateFactory _aggregateFactory;
        private readonly IEventStore _eventStore;
        private readonly ISnapshotStore _snapshotStore;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public AggregateStore(
            IAggregateFactory aggregateFactory,
            IEventStore eventStore,
            ISnapshotStore snapshotStore,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
        {
            _aggregateFactory = aggregateFactory;
            _eventStore = eventStore;
            _snapshotStore = snapshotStore;
            _transientFaultHandler = transientFaultHandler;
        }

        public async Task<TAggregate> LoadAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = await _aggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(id).ConfigureAwait(false);
            await aggregate.LoadAsync(_eventStore, cancellationToken).ConfigureAwait(false);
            return aggregate;
        }

        public TAggregate Load<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = default(TAggregate);

            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadAsync<TAggregate, TIdentity>(id, cancellationToken), r => aggregate = r);
            }

            return aggregate;
        }

        public Task<IReadOnlyCollection<IDomainEvent>> UpdateAsync<TAggregate, TIdentity>(
            TIdentity id,
            ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return _transientFaultHandler.TryAsync(
                async c =>
                {
                    var aggregate = await LoadAsync<TAggregate, TIdentity>(id, c).ConfigureAwait(false);
                    if (aggregate.HasSourceId(sourceId))
                    {
                        throw new DuplicateOperationException(
                            sourceId,
                            id,
                            $"Aggregate '{typeof(TAggregate).PrettyPrint()}' has already had operation '{sourceId}' performed");
                    }

                    await updateAggregate(aggregate, c).ConfigureAwait(false);

                    return await StoreAsync<TAggregate, TIdentity>(aggregate, sourceId, c).ConfigureAwait(false);
                },
                Label.Named("aggregate-update"),
                cancellationToken);
        }

        public Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate, TIdentity>(
            TAggregate aggregate,
            ISourceId sourceId,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return aggregate.CommitAsync(_eventStore, sourceId, cancellationToken);
        }
    }
}