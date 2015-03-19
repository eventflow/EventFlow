// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using System.Linq;
using System.Threading.Tasks;

namespace EventFlow.EventStores
{
    public abstract class EventStore : IEventStore
    {
        public abstract Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate>(
            string id,
            int oldVersion,
            int newVersion,
            IReadOnlyCollection<IUncommittedDomainEvent> uncommittedDomainEvents)
            where TAggregate : IAggregateRoot;

        public abstract Task<IReadOnlyCollection<IDomainEvent>> LoadEventsAsync(string id);

        public virtual async Task<TAggregate> LoadAggregateAsync<TAggregate>(string id)
            where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof(TAggregate);
            var domainEvents = await LoadEventsAsync(id).ConfigureAwait(false);
            var aggregate = (TAggregate)Activator.CreateInstance(aggregateType, id);
            aggregate.ApplyEvents(domainEvents.Select(e => e.GetAggregateEvent()));
            return aggregate;
        }
    }
}
