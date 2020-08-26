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
using System.Collections;
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
using Newtonsoft.Json;

namespace EventFlow.EventStores.InMemory
{
    public class InMemoryEventPersistence : IEventPersistence, IDisposable
    {
        private readonly ILog _log;

        private readonly ConcurrentDictionary<string, ImmutableEventCollection> _eventStore =
            new ConcurrentDictionary<string, ImmutableEventCollection>();

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
                    .AppendLineFormat("{0} v{1} ==================================", AggregateName,
                        AggregateSequenceNumber)
                    .AppendLine(PrettifyJson(Metadata))
                    .AppendLine("---------------------------------")
                    .AppendLine(PrettifyJson(Data))
                    .Append("---------------------------------")
                    .ToString();
            }

            private static string PrettifyJson(string json)
            {
                try
                {
                    var obj = JsonConvert.DeserializeObject(json);
                    var prettyJson = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    return prettyJson;
                }
                catch (Exception)
                {
                    return json;
                }
            }
        }

        private class ImmutableEventCollection : IReadOnlyCollection<InMemoryCommittedDomainEvent>
        {
            private readonly List<InMemoryCommittedDomainEvent> _events;

            public ImmutableEventCollection(List<InMemoryCommittedDomainEvent> events)
            {
                _events = events;
            }

            public int Count => _events.Count;

            public InMemoryCommittedDomainEvent Last => _events.Last();

            public ImmutableEventCollection Add(List<InMemoryCommittedDomainEvent> events)
            {
                return new ImmutableEventCollection(_events.Concat(events).ToList());
            }

            public IEnumerator<InMemoryCommittedDomainEvent> GetEnumerator()
            {
                return _events.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
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
            var startPosition = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);

            var committedDomainEvents = _eventStore
                .SelectMany(kv => kv.Value)
                .Where(e => e.GlobalSequenceNumber >= startPosition)
                .OrderBy(e => e.GlobalSequenceNumber)
                .Take(pageSize)
                .ToList();

            var nextPosition = committedDomainEvents.Any()
                ? committedDomainEvents.Max(e => e.GlobalSequenceNumber) + 1
                : startPosition;

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
                var globalCount = _eventStore.Values.Sum(events => events.Count);

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
                            _log.Verbose("Committing event {0}{1}", Environment.NewLine, committedDomainEvent);
                            return committedDomainEvent;
                        })
                    .ToList();

                var expectedVersion = newCommittedDomainEvents.First().AggregateSequenceNumber - 1;
                var lastEvent = newCommittedDomainEvents.Last();

                var updateResult = _eventStore.AddOrUpdate(id.Value, s => new ImmutableEventCollection(newCommittedDomainEvents),
                    (s, collection) => collection.Count == expectedVersion 
                        ? collection.Add(newCommittedDomainEvents) 
                        : collection);

                if (updateResult.Last != lastEvent)
                {
                    throw new OptimisticConcurrencyException(string.Empty);
                }

                return newCommittedDomainEvents;
            }
        }

        public Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            IReadOnlyCollection<ICommittedDomainEvent> result;

            if (_eventStore.TryGetValue(id.Value, out var committedDomainEvent))
                result = fromEventSequenceNumber <= 1
                    ? (IReadOnlyCollection<ICommittedDomainEvent>) committedDomainEvent
                    : committedDomainEvent.Where(e => e.AggregateSequenceNumber >= fromEventSequenceNumber).ToList();
            else
                result = new List<InMemoryCommittedDomainEvent>();

            return Task.FromResult(result);
        }

        public Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            var deleted = _eventStore.TryRemove(id.Value, out var committedDomainEvents);

            if (deleted)
            {
                _log.Verbose(
                    "Deleted entity with ID '{0}' by deleting all of its {1} events",
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