﻿// The MIT License (MIT)
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
using EventFlow.Logging;

namespace EventFlow.EventStores.InMemory
{
    public class InMemoryEventPersistence : IEventPersistence, IDisposable
    {
        private readonly ILog _log;
        private readonly ConcurrentDictionary<string, List<InMemoryCommittedDomainEvent>> _eventStore = new ConcurrentDictionary<string, List<InMemoryCommittedDomainEvent>>();
        private readonly AsyncLock _asyncLock = new AsyncLock();

        private class InMemoryCommittedDomainEvent : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
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

        public InMemoryEventPersistence(
            ILog log)
        {
            _log = log;
        }

        public Task<AllCommittedEventsPage> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPostion = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);
            var endPosition = startPostion + pageSize;

            var committedDomainEvents = _eventStore
                .SelectMany(kv => kv.Value)
                .Where(e => e.GlobalSequenceNumber >= startPostion && e.GlobalSequenceNumber <= endPosition)
                .ToList();

            var nextPosition = committedDomainEvents.Any()
                ? committedDomainEvents.Max(e => e.GlobalSequenceNumber) + 1
                : startPostion;

            return Task.FromResult(new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), committedDomainEvents));
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
            IIdentity id,
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

                List<InMemoryCommittedDomainEvent> committedDomainEvents;
                if (_eventStore.ContainsKey(id.Value))
                {
                    committedDomainEvents = _eventStore[id.Value];
                }
                else
                {
                    committedDomainEvents = new List<InMemoryCommittedDomainEvent>();
                    _eventStore[id.Value] = committedDomainEvents;
                }

                var newCommittedDomainEvents = serializedEvents
                    .Select((e, i) =>
                        {
                            var committedDomainEvent = new InMemoryCommittedDomainEvent
                                {
                                    AggregateId = id.Value,
                                    AggregateName = e.Metadata[MetadataKeys.AggregateName],
                                    AggregateSequenceNumber = e.AggregateSequenceNumber,
                                    Data = e.SerializedData,
                                    Metadata = e.SerializedMetadata,
                                    GlobalSequenceNumber = globalCount + i + 1,
                                };
                            _log.TraceFormat("Committing event {0}{1}", Environment.NewLine, committedDomainEvent.ToString());
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

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                List<InMemoryCommittedDomainEvent> committedDomainEvent;
                return _eventStore.TryGetValue(id.Value, out committedDomainEvent)
                    ? fromEventSequenceNumber <= 1 ? committedDomainEvent : committedDomainEvent.Where(e => e.AggregateSequenceNumber >= fromEventSequenceNumber).ToList()
                    : new List<InMemoryCommittedDomainEvent>();
            }
        }

        public Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            if (!_eventStore.ContainsKey(id.Value))
            {
                return Task.FromResult(0);
            }

            List<InMemoryCommittedDomainEvent> committedDomainEvents;
            _eventStore.TryRemove(id.Value, out committedDomainEvents);

            _log.TraceFormat(
                "Deleted entity with ID '{0}' by deleting all of its {1} events",
                id,
                committedDomainEvents.Count);

            return Task.FromResult(0);
        }

        public void Dispose()
        {
            _asyncLock.Dispose();
        }
    }
}