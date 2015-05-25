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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.EventStores.InMemory
{
    public class InMemoryEventStore : EventStore, IDisposable
    {
        private readonly ConcurrentDictionary<string, List<ICommittedDomainEvent>> _eventStore = new ConcurrentDictionary<string, List<ICommittedDomainEvent>>();
        private readonly AsyncLock _asyncLock = new AsyncLock();

        private class InMemoryCommittedDomainEvent : ICommittedDomainEvent
        {
            public string AggregateId { get; set; }
            public string AggregateName { private get; set; }
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
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEnumerable<IMetadataProvider> metadataProviders,
            IEventUpgradeManager eventUpgradeManager)
            : base(log, aggregateFactory, eventJsonSerializer, eventUpgradeManager, metadataProviders)
        {
        }

        protected async override Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return new List<ICommittedDomainEvent>();
            }

            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var globalCount = _eventStore.Values.SelectMany(e => e).Count();

                List<ICommittedDomainEvent> committedDomainEvents;
                if (_eventStore.ContainsKey(id.Value))
                {
                    committedDomainEvents = _eventStore[id.Value];
                }
                else
                {
                    committedDomainEvents = new List<ICommittedDomainEvent>();
                    _eventStore[id.Value] = committedDomainEvents;
                }

                var newCommittedDomainEvents = serializedEvents
                    .Select((e, i) =>
                        {
                            var committedDomainEvent = (ICommittedDomainEvent) new InMemoryCommittedDomainEvent
                                {
                                    AggregateId = id.Value,
                                    AggregateName = e.Metadata[MetadataKeys.AggregateName],
                                    AggregateSequenceNumber = e.AggregateSequenceNumber,
                                    Data = e.Data,
                                    Metadata = e.Meta,
                                };
                            Log.Verbose("Committing event {0}{1}", Environment.NewLine, committedDomainEvent.ToString());
                            return committedDomainEvent;
                        })
                    .ToList();

                var expectedVersion = newCommittedDomainEvents.First().AggregateSequenceNumber - 1;
                if (expectedVersion != committedDomainEvents.Count)
                {
                    throw new OptimisticConcurrencyException("");
                }

                committedDomainEvents.AddRange(newCommittedDomainEvents);

                return newCommittedDomainEvents;
            }
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                List<ICommittedDomainEvent> committedDomainEvent;
                return _eventStore.TryGetValue(id.Value, out committedDomainEvent)
                    ? committedDomainEvent
                    : new List<ICommittedDomainEvent>();
            }
        }

        public override Task DeleteAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
        {
            if (_eventStore.ContainsKey(id.Value))
            {
                List<ICommittedDomainEvent> committedDomainEvents;
                _eventStore.TryRemove(id.Value, out committedDomainEvents);
                Log.Verbose(
                    "Deleted aggregate '{0}' with ID '{1}' by deleting all of its {2} events",
                    typeof(TAggregate).Name,
                    id,
                    committedDomainEvents.Count);
            }

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _asyncLock.Dispose();
        }
    }
}
