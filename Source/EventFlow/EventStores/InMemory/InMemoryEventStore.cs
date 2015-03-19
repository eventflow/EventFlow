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

namespace EventFlow.EventStores.InMemory
{
    public class InMemoryEventStore : EventStore
    {
        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly Dictionary<string, List<IDomainEvent>> _eventStore = new Dictionary<string, List<IDomainEvent>>();

        private class InMemoryCommittedDomainEvent : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public InMemoryEventStore(
            IEventJsonSerializer eventJsonSerializer)
        {
            _eventJsonSerializer = eventJsonSerializer;
        }

        public override Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate>(
            string id,
            int oldVersion,
            int newVersion,
            IReadOnlyCollection<IUncommittedDomainEvent> uncommittedDomainEvents)
        {
            var globalCount = _eventStore.Values.SelectMany(e => e).Count();
            var batchId = Guid.NewGuid();

            List<IDomainEvent> domainEvents;
            if (_eventStore.ContainsKey(id))
            {
                domainEvents = _eventStore[id];
            }
            else
            {
                domainEvents = new List<IDomainEvent>();
                _eventStore[id] = domainEvents;
            }

            var committedDomainEvents = uncommittedDomainEvents
                .Select(_eventJsonSerializer.Serialize)
                .Select((e, i) => new InMemoryCommittedDomainEvent
                    {
                        AggregateId = id,
                        AggregateName = typeof(TAggregate).Name,
                        AggregateSequenceNumber = domainEvents.Count + i + 1,
                        BatchId = batchId,
                        Data = e.Data,
                        Metadata = e.Meta,
                        GlobalSequenceNumber = globalCount + i + 1
                    })
                .ToList();
            var newDomainEvents = committedDomainEvents.Select(_eventJsonSerializer.Deserialize).ToList();
            domainEvents.AddRange(newDomainEvents);
            return Task.FromResult<IReadOnlyCollection<IDomainEvent>>(newDomainEvents);
        }

        public override Task<IReadOnlyCollection<IDomainEvent>> LoadEventsAsync(string id)
        {
            var domainEvents = _eventStore.ContainsKey(id)
                ? _eventStore[id]
                : new List<IDomainEvent>();
            return Task.FromResult<IReadOnlyCollection<IDomainEvent>>(domainEvents);
        }
    }
}
