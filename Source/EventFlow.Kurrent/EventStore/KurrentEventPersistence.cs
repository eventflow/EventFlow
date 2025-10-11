// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using KurrentDB.Client;
using Microsoft.Extensions.Logging;

namespace EventFlow.Kurrent.EventStore
{
    public class KurrentEventPersistence : IEventPersistence
    {
        private readonly KurrentDBClient _client;
        private readonly ILogger<KurrentEventPersistence> _logger;

        private sealed class KurrentCommittedDomainEvent : ICommittedDomainEvent
        {
            public string AggregateId { get; init; } = string.Empty;
            public string Data { get; init; } = string.Empty;
            public string Metadata { get; init; } = string.Empty;
            public int AggregateSequenceNumber { get; init; }
        }

        public KurrentEventPersistence(KurrentDBClient client, ILogger<KurrentEventPersistence> logger)
        {
            _client = client;
            _logger = logger;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
        {
            if (pageSize <= 0)
            {
                return new AllCommittedEventsPage(globalPosition, Array.Empty<ICommittedDomainEvent>());
            }

            var startPosition = ParsePosition(globalPosition);
            var events = new List<ResolvedEvent>(capacity: pageSize);
            Position? lastPosition = null;
            Position? firstPositionToSkip = globalPosition.IsStart ? null : startPosition;

            var read = _client.ReadAllAsync(Direction.Forwards, startPosition, cancellationToken: cancellationToken);

            await foreach (var resolved in read.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (firstPositionToSkip.HasValue && AreSamePosition(resolved.Event.Position, firstPositionToSkip.Value))
                {
                    firstPositionToSkip = null;
                    continue;
                }

                if (IsSystemEvent(resolved.Event.EventType))
                {
                    continue;
                }

                events.Add(resolved);
                lastPosition = resolved.Event.Position;

                if (events.Count >= pageSize)
                {
                    break;
                }
            }

            var committedEvents = Map(events);
            var nextGlobalPosition = lastPosition.HasValue
                ? new GlobalPosition(FormattableString.Invariant($"{lastPosition.Value.CommitPosition}-{lastPosition.Value.PreparePosition}"))
                : globalPosition;

            return new AllCommittedEventsPage(nextGlobalPosition, committedEvents);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            var orderedEvents = serializedEvents
                .OrderBy(e => e.AggregateSequenceNumber)
                .ToList();

            var committedEvents = orderedEvents
                .Select(e => new KurrentCommittedDomainEvent
                {
                    AggregateId = id.Value,
                    AggregateSequenceNumber = e.AggregateSequenceNumber,
                    Data = e.SerializedData,
                    Metadata = e.SerializedMetadata
                })
                .ToList();

            var eventData = orderedEvents
                .Select(e =>
                {
                    var eventType = FormattableString.Invariant($"{e.Metadata[MetadataKeys.AggregateName]}.{e.Metadata.EventName}.{e.Metadata.EventVersion}");
                    return new EventData(
                        Uuid.NewUuid(),
                        eventType,
                        Encoding.UTF8.GetBytes(e.SerializedData),
                        Encoding.UTF8.GetBytes(e.SerializedMetadata));
                })
                .ToArray();

            var firstSequenceNumber = orderedEvents.First().AggregateSequenceNumber;

            try
            {
                if (firstSequenceNumber <= 1)
                {
                    await _client
                        .AppendToStreamAsync(id.Value, StreamState.NoStream, eventData, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }
                else
                {
                    var expectedRevision = (ulong)(firstSequenceNumber - 2);
                    await _client
                        .AppendToStreamAsync(id.Value, expectedRevision, eventData, cancellationToken: cancellationToken)
                        .ConfigureAwait(false);
                }

                var lastSequenceNumber = committedEvents[^1].AggregateSequenceNumber;
                _logger.LogDebug("Appended {EventCount} events to KurrentDB stream {StreamId} ending at sequence {SequenceNumber}", committedEvents.Count, id.Value, lastSequenceNumber);
            }
            catch (WrongExpectedVersionException exception)
            {
                throw new OptimisticConcurrencyException(exception.Message, exception);
            }

            return committedEvents;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
        {
            var start = ResolveStreamPosition(fromEventSequenceNumber);
            var readResult = _client.ReadStreamAsync(Direction.Forwards, id.Value, start, cancellationToken: cancellationToken);

            if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            var resolvedEvents = new List<ResolvedEvent>();

            await foreach (var resolved in readResult.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                if (ToSequenceNumber(resolved) >= fromEventSequenceNumber)
                {
                    resolvedEvents.Add(resolved);
                }
            }

            return Map(resolvedEvents);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, int toEventSequenceNumber, CancellationToken cancellationToken)
        {
            if (toEventSequenceNumber < fromEventSequenceNumber)
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            var maxCount = (long)(toEventSequenceNumber - fromEventSequenceNumber + 1);
            var start = ResolveStreamPosition(fromEventSequenceNumber);
            var readResult = _client.ReadStreamAsync(Direction.Forwards, id.Value, start, maxCount: maxCount, cancellationToken: cancellationToken);

            if (await readResult.ReadState.ConfigureAwait(false) == ReadState.StreamNotFound)
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            var resolvedEvents = new List<ResolvedEvent>();

            await foreach (var resolved in readResult.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                var sequenceNumber = ToSequenceNumber(resolved);
                if (sequenceNumber < fromEventSequenceNumber)
                {
                    continue;
                }

                if (sequenceNumber > toEventSequenceNumber)
                {
                    break;
                }

                resolvedEvents.Add(resolved);

                if (resolvedEvents.Count >= maxCount)
                {
                    break;
                }
            }

            return Map(resolvedEvents);
        }

        public Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Soft deleting KurrentDB stream {StreamId}", id.Value);
            return _client.DeleteAsync(id.Value, StreamState.Any, cancellationToken: cancellationToken);
        }

        private static IReadOnlyCollection<ICommittedDomainEvent> Map(IEnumerable<ResolvedEvent> resolvedEvents)
        {
            return resolvedEvents
                .Select(resolved => new KurrentCommittedDomainEvent
                {
                    AggregateId = resolved.Event.EventStreamId,
                    AggregateSequenceNumber = (int) ToSequenceNumber(resolved),
                    Data = Encoding.UTF8.GetString(resolved.Event.Data.Span),
                    Metadata = Encoding.UTF8.GetString(resolved.Event.Metadata.Span)
                })
                .ToList();
        }

        private static StreamPosition ResolveStreamPosition(int fromEventSequenceNumber)
        {
            return fromEventSequenceNumber <= 1
                ? StreamPosition.Start
                : StreamPosition.FromInt64(fromEventSequenceNumber - 1);
        }

        private static Position ParsePosition(GlobalPosition globalPosition)
        {
            if (globalPosition.IsStart)
            {
                return Position.Start;
            }

            var parts = globalPosition.Value.Split('-');
            if (parts.Length != 2)
            {
                throw new ArgumentException(FormattableString.Invariant($"Invalid global position '{globalPosition.Value}'. Expected '<commit>-<prepare>'."), nameof(globalPosition));
            }

            if (!ulong.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var commitPosition) ||
                !ulong.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var preparePosition))
            {
                throw new ArgumentException(FormattableString.Invariant($"Invalid global position '{globalPosition.Value}'. Expected '<commit>-<prepare>'."), nameof(globalPosition));
            }

            return new Position(commitPosition, preparePosition);
        }

        private static bool AreSamePosition(Position actual, Position expected)
        {
            return actual.CommitPosition == expected.CommitPosition && actual.PreparePosition == expected.PreparePosition;
        }

        private static bool IsSystemEvent(string eventType)
        {
            return eventType.Length > 0 && eventType[0] == '$';
        }

        private static long ToSequenceNumber(ResolvedEvent resolved)
        {
            // Stream revisions are zero-based in KurrentDB, EventFlow expects one-based.
            return (long) (ulong)(resolved.Event.EventNumber + 1);
        }
    }
}
