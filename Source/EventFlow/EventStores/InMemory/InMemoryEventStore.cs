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
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.EventStores.InMemory
{
    public class InMemoryEventStore : EventStore
    {
        private readonly Dictionary<string, List<ICommittedDomainEvent>> _eventStore = new Dictionary<string, List<ICommittedDomainEvent>>();

        private class InMemoryCommittedDomainEvent : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }

            public override string ToString()
            {
                return new StringBuilder()
                    .AppendLineFormat("{0} v{1} ==================================", AggregateName, AggregateSequenceNumber)
                    .AppendLine(Metadata)
                    .AppendLine("---------------------------------")
                    .AppendLine(Data)
                    .Append("---------------------------------")
                    .ToString();
            }
        }

        public InMemoryEventStore(
            ILog log,
            IEventJsonSerializer eventJsonSerializer,
            IEnumerable<IMetadataProvider> metadataProviders)
            : base(log, eventJsonSerializer, metadataProviders)
        {
        }

        protected override Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate>(string id, IReadOnlyCollection<SerializedEvent> serializedEvents)
        {
            var globalCount = _eventStore.Values.SelectMany(e => e).Count();
            var batchId = Guid.NewGuid();

            List<ICommittedDomainEvent> committedDomainEvents;
            if (_eventStore.ContainsKey(id))
            {
                committedDomainEvents = _eventStore[id];
            }
            else
            {
                committedDomainEvents = new List<ICommittedDomainEvent>();
                _eventStore[id] = committedDomainEvents;
            }

            var newCommittedDomainEvents = serializedEvents
                .Select((e, i) =>
                    {
                        var committedDomainEvent = (ICommittedDomainEvent) new InMemoryCommittedDomainEvent
                            {
                                AggregateId = id,
                                AggregateName = typeof (TAggregate).Name,
                                AggregateSequenceNumber = e.AggregateSequenceNumber,
                                BatchId = batchId,
                                Data = e.Data,
                                Metadata = e.Meta,
                                GlobalSequenceNumber = globalCount + i + 1
                            };
                        Log.Verbose("Committing event {0}{1}", Environment.NewLine, committedDomainEvent.ToString());
                        return committedDomainEvent;
                    })
                .ToList();
            committedDomainEvents.AddRange(newCommittedDomainEvents);

            return Task.FromResult<IReadOnlyCollection<ICommittedDomainEvent>>(newCommittedDomainEvents);
        }

        protected override Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(string id)
        {
            var committedDomainEvents = _eventStore.ContainsKey(id)
                ? _eventStore[id]
                : new List<ICommittedDomainEvent>();
            return Task.FromResult<IReadOnlyCollection<ICommittedDomainEvent>>(committedDomainEvents);
        }
    }
}
