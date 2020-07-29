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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.Extensions
{
    public static class AggregateStoreExtensions
    {
        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static TAggregate Load<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TIdentity id)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return aggregateStore.Load<TAggregate, TIdentity>(id, CancellationToken.None);
        }

        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static TAggregate Load<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = default(TAggregate);

            using (var a = AsyncHelper.Wait)
            {
                a.Run(aggregateStore.LoadAsync<TAggregate, TIdentity>(id, cancellationToken), r => aggregate = r);
            }

            return aggregate;
        }

        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static IReadOnlyCollection<IDomainEvent> Update<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TIdentity id,
            ISourceId sourceId,
            Action<TAggregate> updateAggregate)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return aggregateStore.Update<TAggregate, TIdentity>(
                id,
                sourceId,
                (a, c) =>
                    {
                        updateAggregate(a);
                        return Task.FromResult(0);
                    },
                CancellationToken.None);
        }

        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static IReadOnlyCollection<IDomainEvent> Update<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TIdentity id,
            ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            IReadOnlyCollection<IDomainEvent> domainEvents = null;

            using (var a = AsyncHelper.Wait)
            {
                a.Run(aggregateStore.UpdateAsync(id, sourceId, updateAggregate, cancellationToken), r => domainEvents = r);
            }

            return domainEvents;
        }

        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static IReadOnlyCollection<IDomainEvent> Store<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TAggregate aggregate,
            ISourceId sourceId)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return aggregateStore.Store<TAggregate, TIdentity>(aggregate, sourceId, CancellationToken.None);
        }

        [Obsolete("Non-async extension methods will all be removed in EventFlow 1.0, use async methods instead")]
        public static IReadOnlyCollection<IDomainEvent> Store<TAggregate, TIdentity>(
            this IAggregateStore aggregateStore,
            TAggregate aggregate,
            ISourceId sourceId,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            IReadOnlyCollection<IDomainEvent> domainEvents = null;

            using (var a = AsyncHelper.Wait)
            {
                a.Run(aggregateStore.StoreAsync<TAggregate, TIdentity>(aggregate, sourceId, cancellationToken), r => domainEvents = r);
            }

            return domainEvents;
        }
    }
}