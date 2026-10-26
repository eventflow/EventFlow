// The MIT License (MIT)
//
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Logs;
using Grpc.Core;
using KurrentDB.Client;

namespace EventFlow.EventStores.KurrentDB
{
    public class KurrentDBEventPersistence : IEventPersistence
    {
        private const int MaxAppendAttempts = 5;
        private static readonly TimeSpan AppendDeadline = TimeSpan.FromSeconds(30);
        private readonly Func<string, StreamState, EventData[], CancellationToken, Task> _append;
        private readonly ILog _log;
        private readonly KurrentDBClient _client;

        private class KurrentDBEvent : ICommittedDomainEvent
        {
            public string AggregateId { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public KurrentDBEventPersistence(ILog log, KurrentDBClient client)
            : this(log, client, null)
        {
        }

        // Internal transport seam allows tests to reproduce a lost acknowledgement
        // after a real server commit without changing the public registration API.
        internal KurrentDBEventPersistence(ILog log, KurrentDBClient client,
            Func<string, StreamState, EventData[], CancellationToken, Task> append)
        {
            _log = log;
            _client = client;
            _append = append ?? ((stream, expectedState, events, token) => client.AppendToStreamAsync(
                stream, expectedState, events,
                deadline: AppendDeadline,
                configureOperationOptions: options => options.ThrowOnAppendFailure = true,
                cancellationToken: token));
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
            IIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (serializedEvents.Count == 0)
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            var firstSequenceNumber = serializedEvents.Min(e => e.AggregateSequenceNumber);
            var expectedState = firstSequenceNumber <= 1
                ? StreamState.NoStream
                : StreamState.StreamRevision((ulong)(firstSequenceNumber - 2));
            var events = serializedEvents.Select(e => new EventData(
                // Use a fresh storage ID, as in the TCP adapter, so a repeated
                // stale commit cannot be accepted as an idempotent write.
                Uuid.NewUuid(),
                string.Format("{0}.{1}.{2}", e.Metadata.AggregateName, e.Metadata.EventName, e.Metadata.EventVersion),
                Encoding.UTF8.GetBytes(e.SerializedData),
                Encoding.UTF8.GetBytes(e.SerializedMetadata),
                "application/json")).ToArray();

            try
            {
                await AppendWithRetryAsync(id.Value, expectedState, events, cancellationToken).ConfigureAwait(false);
            }
            catch (WrongExpectedVersionException e)
            {
                throw new OptimisticConcurrencyException(e.Message, e);
            }

            _log.Verbose("Wrote entity {0} with sequence number {1}",
                id, serializedEvents.Max(e => e.AggregateSequenceNumber));

            return serializedEvents.Select(e => (ICommittedDomainEvent)new KurrentDBEvent
            {
                AggregateId = id.Value,
                AggregateSequenceNumber = e.AggregateSequenceNumber,
                Data = e.SerializedData,
                Metadata = e.SerializedMetadata
            }).ToList();
        }

        private async Task AppendWithRetryAsync(string stream, StreamState expectedState,
            EventData[] events, CancellationToken cancellationToken)
        {
            // A deadline does not establish whether the server committed the write.
            // Retain the exact batch, storage IDs and expected revision on every retry.
            // IDs are fresh again on a separate CommitEventsAsync call, so rival
            // commands at the same aggregate sequence are still concurrency conflicts.
            for (var attempt = 1; ; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await _append(stream, expectedState, events, cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (RpcException exception) when (exception.StatusCode == StatusCode.DeadlineExceeded
                    && attempt < MaxAppendAttempts && !cancellationToken.IsCancellationRequested)
                {
                    _log.Warning("Append to stream '{0}' timed out on attempt {1}/{2}; retrying the identical batch because the outcome is unknown",
                        stream, attempt, MaxAppendAttempts);
                    await Task.Delay(TimeSpan.FromMilliseconds(50 * attempt), cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var start = fromEventSequenceNumber <= 1
                ? StreamPosition.Start
                : new StreamPosition((ulong)(fromEventSequenceNumber - 1));
            var events = new List<ICommittedDomainEvent>();
            var read = _client.ReadStreamAsync(
                Direction.Forwards,
                id.Value,
                start,
                resolveLinkTos: false,
                cancellationToken: cancellationToken);

            try
            {
                await foreach (var resolvedEvent in read.ConfigureAwait(false))
                {
                    events.Add(Map(resolvedEvent));
                }
            }
            catch (StreamNotFoundException)
            {
                return Array.Empty<ICommittedDomainEvent>();
            }

            return events;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (pageSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pageSize));
            }

            var position = ParsePosition(globalPosition, out var exclusive);
            var nextPosition = globalPosition;
            var events = new List<ICommittedDomainEvent>();
            var read = _client.ReadAllAsync(Direction.Forwards, position,
                resolveLinkTos: false, cancellationToken: cancellationToken);

            await foreach (var resolvedEvent in read.ConfigureAwait(false))
            {
                var currentPosition = resolvedEvent.OriginalPosition.Value;
                // Legacy TCP checkpoints identify the next unread location and
                // are inclusive. Our versioned checkpoints identify the last
                // scanned record, since gRPC does not expose TCP's NextPosition.
                if (exclusive && currentPosition <= position)
                {
                    continue;
                }

                nextPosition = new GlobalPosition(string.Format(CultureInfo.InvariantCulture,
                    "after:{0}-{1}", currentPosition.CommitPosition, currentPosition.PreparePosition));
                if (resolvedEvent.OriginalStreamId.StartsWith("$", StringComparison.Ordinal))
                {
                    continue;
                }

                events.Add(Map(resolvedEvent));
                if (events.Count == pageSize)
                {
                    break;
                }
            }

            return new AllCommittedEventsPage(nextPosition, events);
        }

        private static Position ParsePosition(GlobalPosition globalPosition, out bool exclusive)
        {
            if (globalPosition == null)
            {
                throw new ArgumentNullException(nameof(globalPosition));
            }

            exclusive = false;
            if (globalPosition.IsStart)
            {
                return Position.Start;
            }

            const string prefix = "after:";
            exclusive = globalPosition.Value.StartsWith(prefix, StringComparison.Ordinal);
            var value = exclusive ? globalPosition.Value.Substring(prefix.Length) : globalPosition.Value;
            var parts = value.Split('-');
            if (parts.Length != 2
                || !long.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var commit)
                || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var prepare)
                || commit < prepare)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "Invalid global position '{0}'. Expected 'commit-prepare' or 'after:commit-prepare' with non-negative 64-bit positions and commit >= prepare.",
                    globalPosition.Value), nameof(globalPosition));
            }

            return new Position((ulong)commit, (ulong)prepare);
        }

        private static ICommittedDomainEvent Map(ResolvedEvent resolvedEvent)
        {
            var record = resolvedEvent.Event;
            return new KurrentDBEvent
            {
                AggregateId = record.EventStreamId,
                AggregateSequenceNumber = checked((int)(record.EventNumber.ToUInt64() + 1)),
                Data = Encoding.UTF8.GetString(record.Data.ToArray()),
                Metadata = Encoding.UTF8.GetString(record.Metadata.ToArray())
            };
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _client.DeleteAsync(id.Value, StreamState.Any,
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }
}
