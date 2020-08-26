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
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Extensions
{
    public static class EventStoreExtensions
    {
        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0,\r\nuse async methods instead")]
        public static IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> LoadEvents<TAggregate, TIdentity>(
            this IEventStore eventStore,
            TIdentity id)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return eventStore.LoadEvents<TAggregate, TIdentity>(id, CancellationToken.None);
        }

        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0,\r\nuse async methods instead")]
        public static IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> LoadEvents<TAggregate, TIdentity>(
            this IEventStore eventStore,
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> domainEvents = null;
            using (var a = AsyncHelper.Wait)
            {
                a.Run(eventStore.LoadEventsAsync<TAggregate, TIdentity>(id, cancellationToken), d => domainEvents = d);
            }
            return domainEvents;
        }

        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0,\r\nuse async methods instead")]
        public static AllEventsPage LoadAllEvents(
            this IEventStore eventStore,
            GlobalPosition globalPosition,
            int pageSize)
        {
            return eventStore.LoadAllEvents(globalPosition, pageSize, CancellationToken.None);
        }
        
        [Obsolete("Non-async extensions methods will all be removed in EventFlow 1.0,\r\nuse async methods instead")]
        public static AllEventsPage LoadAllEvents(
            this IEventStore eventStore,
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            AllEventsPage allEventsPage = null;
            using (var a = AsyncHelper.Wait)
            {
                a.Run(eventStore.LoadAllEventsAsync(globalPosition, pageSize, cancellationToken), p => allEventsPage = p);
            }
            return allEventsPage;
        }

        [Obsolete("Use IAggregateStore.Load instead")]
        public static TAggregate LoadAggregate<TAggregate, TIdentity>(
            this IEventStore eventStore,
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = default(TAggregate);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(eventStore.LoadAggregateAsync<TAggregate, TIdentity>(id, cancellationToken), r => aggregate = r);
            }
            return aggregate;
        }
    }
}