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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.EventStores.KurrentDB.Extensions;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using KurrentDB.Client;
using NUnit.Framework;

namespace EventFlow.EventStores.KurrentDB.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Categories.Integration)]
    public class KurrentDBEventPersistenceTests
    {
        private KurrentDBClient _client;
        private KurrentDBEventPersistence _persistence;
        private ThingyId _id;
        private CancellationTokenSource _timeout;

        [SetUp]
        public void SetUp()
        {
            var connectionString = TestConnectionStrings.Persistence;
            var settings = KurrentDBClientSettings.Create(connectionString);
            // The adapter must enforce optimistic concurrency even when the
            // application's shared client does not throw on append failure.
            settings.OperationOptions.ThrowOnAppendFailure = false;
            _client = new KurrentDBClient(settings);
            _persistence = new KurrentDBEventPersistence(new NullLog(), _client);
            _id = ThingyId.New;
            _timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        }

        [TearDown]
        public void TearDown()
        {
            _client.Dispose();
            _timeout.Dispose();
        }

        [Test]
        public async Task CommitPreservesSerializedDataMetadataAndEventType()
        {
            var serialized = CreateEvent(1);
            var committed = await _persistence.CommitEventsAsync(_id, new[] { serialized }, _timeout.Token);
            AssertEvent(committed.Single(), serialized);

            var records = await ReadRawEvents();
            var record = records.Single().Event;
            Assert.That(record.EventType, Is.EqualTo("Thingy.Ping.2"));
            Assert.That(record.ContentType, Is.EqualTo("application/json"));
            Assert.That(record.Data.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes(serialized.SerializedData)));
            Assert.That(record.Metadata.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes(serialized.SerializedMetadata)));

            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            AssertEvent(loaded.Single(), serialized);
        }

        [Test]
        public async Task MultipleCommitsContinueAggregateSequenceNumbers()
        {
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1), CreateEvent(2) }, _timeout.Token);
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(3), CreateEvent(4) }, _timeout.Token);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded.Select(e => e.AggregateSequenceNumber), Is.EqualTo(new[] { 1, 2, 3, 4 }));
        }

        [TestCase(-1, 3)]
        [TestCase(0, 3)]
        [TestCase(1, 3)]
        [TestCase(2, 2)]
        [TestCase(4, 0)]
        public async Task ReadsFromRequestedSequenceNumber(int from, int count)
        {
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1), CreateEvent(2), CreateEvent(3) }, _timeout.Token);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, from, _timeout.Token);
            Assert.That(loaded.Select(e => e.AggregateSequenceNumber),
                Is.EqualTo(Enumerable.Range(Math.Max(1, from), count)));
        }

        [Test]
        public async Task MissingAggregateReturnsNoEvents()
        {
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded, Is.Empty);
        }

        [Test]
        public async Task ReadsMoreThanLegacyPageSize()
        {
            var serialized = Enumerable.Range(1, 405).Select(CreateEvent).ToArray();
            await _persistence.CommitEventsAsync(_id, serialized, _timeout.Token);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded.Select(e => e.AggregateSequenceNumber), Is.EqualTo(Enumerable.Range(1, 405)));
            AssertEvent(loaded.Last(), serialized.Last());
        }

        [Test]
        public async Task ReadsExistingRecordsUsingLegacyEventFormat()
        {
            var serialized = CreateEvent(1);
            await _client.AppendToStreamAsync(_id.Value, StreamState.NoStream,
                new[] { new EventData(Uuid.NewUuid(), "Thingy.Ping.2",
                    Encoding.UTF8.GetBytes(serialized.SerializedData),
                    Encoding.UTF8.GetBytes(serialized.SerializedMetadata)) },
                cancellationToken: _timeout.Token);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            AssertEvent(loaded.Single(), serialized);
        }

        [TestCase(1)]
        [TestCase(2)]
        public async Task RepeatedStaleCommitThrowsOptimisticConcurrencyException(int sequenceNumber)
        {
            if (sequenceNumber > 1)
            {
                await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            }

            var serialized = CreateEvent(sequenceNumber);
            await _persistence.CommitEventsAsync(_id, new[] { serialized }, _timeout.Token);
            var exception = Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await _persistence.CommitEventsAsync(_id, new[] { serialized }, _timeout.Token));
            Assert.That(exception.InnerException, Is.TypeOf<WrongExpectedVersionException>());
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded.Count, Is.EqualTo(sequenceNumber));

            // A rejected write must leave the stream usable at its current revision.
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(sequenceNumber + 1) }, _timeout.Token);
            loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded.Select(e => e.AggregateSequenceNumber),
                Is.EqualTo(Enumerable.Range(1, sequenceNumber + 1)));
        }

        [Test]
        public void AppendingToMissingStreamWithNonInitialSequenceThrows()
        {
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(2) }, _timeout.Token));
        }

        [Test]
        public async Task EmptyCommitDoesNotCreateStream()
        {
            var committed = await _persistence.CommitEventsAsync(_id, Array.Empty<SerializedEvent>(), _timeout.Token);
            Assert.That(committed, Is.Empty);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded, Is.Empty);
        }

        [Test]
        public void PreCancelledOperationsAreCancelled()
        {
            using (var cancellation = new CancellationTokenSource())
            {
                cancellation.Cancel();
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, cancellation.Token));
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await _persistence.LoadCommittedEventsAsync(_id, 1, cancellation.Token));
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await _persistence.DeleteEventsAsync(_id, cancellation.Token));
                Assert.ThrowsAsync<OperationCanceledException>(async () =>
                    await _persistence.LoadAllCommittedEvents(GlobalPosition.Start, 10, cancellation.Token));
            }
        }

        [Test]
        public async Task DeleteRemovesOnlyTheRequestedAggregate()
        {
            var otherId = ThingyId.New;
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1), CreateEvent(2) }, _timeout.Token);
            await _persistence.CommitEventsAsync(otherId, new[] { CreateEvent(1) }, _timeout.Token);
            await _persistence.DeleteEventsAsync(_id, _timeout.Token);

            var deleted = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            var other = await _persistence.LoadCommittedEventsAsync(otherId, 1, _timeout.Token);
            Assert.That(deleted, Is.Empty);
            Assert.That(other.Single().AggregateId, Is.EqualTo(otherId.Value));
        }

        [Test]
        public async Task DeleteIsSoftAndDoesNotTombstoneTheStream()
        {
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            await _persistence.DeleteEventsAsync(_id, _timeout.Token);
            await _client.AppendToStreamAsync(_id.Value, StreamState.Any,
                new[] { new EventData(Uuid.NewUuid(), "Thingy.Ping.2", Encoding.UTF8.GetBytes("{}")) },
                cancellationToken: _timeout.Token);
            var loaded = await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(loaded.Single().AggregateSequenceNumber, Is.EqualTo(2));
        }

        [Test]
        public void DeleteMissingStreamPreservesServerFailure()
        {
            Assert.ThrowsAsync<WrongExpectedVersionException>(async () =>
                await _persistence.DeleteEventsAsync(_id, _timeout.Token));
        }
        [TestCase(1)]
        [TestCase(2)]
        public async Task GlobalPagesDoNotSkipOrRepeatEventsWithinBatch(int pageSize)
        {
            var checkpoint = await TailCheckpoint();
            var serialized = Enumerable.Range(1, 5).Select(CreateEvent).ToArray();
            await _persistence.CommitEventsAsync(_id, serialized, _timeout.Token);
            var loaded = new System.Collections.Generic.List<ICommittedDomainEvent>();
            for (var pageIndex = 0; pageIndex < 6; pageIndex++)
            {
                var page = await _persistence.LoadAllCommittedEvents(checkpoint, pageSize, _timeout.Token);
                Assert.That(page.CommittedDomainEvents.Count, Is.LessThanOrEqualTo(pageSize));
                Assert.That(page.NextGlobalPosition.Value, Does.StartWith("after:"));
                loaded.AddRange(page.CommittedDomainEvents);
                checkpoint = page.NextGlobalPosition;
                if (page.CommittedDomainEvents.Count == 0) break;
            }

            Assert.That(loaded.Select(e => e.AggregateSequenceNumber), Is.EqualTo(Enumerable.Range(1, 5)));
            for (var i = 0; i < serialized.Length; i++) AssertEvent(loaded[i], serialized[i]);
        }

        [Test]
        public async Task LegacyCheckpointIsInclusiveAndNewCheckpointIsExclusive()
        {
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1), CreateEvent(2), CreateEvent(3) }, _timeout.Token);
            var records = await ReadRawEvents();
            var second = records[1].OriginalPosition.Value;
            var checkpoint = new GlobalPosition(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "{0}-{1}", second.CommitPosition, second.PreparePosition));
            var page = await _persistence.LoadAllCommittedEvents(checkpoint, 1, _timeout.Token);
            Assert.That(page.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(2));
            var next = await _persistence.LoadAllCommittedEvents(page.NextGlobalPosition, 1, _timeout.Token);
            Assert.That(next.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(3));
        }

        [Test]
        public async Task TailCheckpointRemainsUsableAfterEmptyReadsAndNewAppends()
        {
            var checkpoint = await TailCheckpoint();
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            var first = await _persistence.LoadAllCommittedEvents(checkpoint, 10, _timeout.Token);
            Assert.That(first.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(1));
            var empty = await _persistence.LoadAllCommittedEvents(first.NextGlobalPosition, 10, _timeout.Token);
            Assert.That(empty.CommittedDomainEvents, Is.Empty);
            var emptyAgain = await _persistence.LoadAllCommittedEvents(empty.NextGlobalPosition, 10, _timeout.Token);
            Assert.That(emptyAgain.CommittedDomainEvents, Is.Empty);
            Assert.That(emptyAgain.NextGlobalPosition, Is.EqualTo(empty.NextGlobalPosition));

            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(2) }, _timeout.Token);
            var appended = await _persistence.LoadAllCommittedEvents(emptyAgain.NextGlobalPosition, 10, _timeout.Token);
            Assert.That(appended.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(2));
        }

        [Test]
        public async Task GlobalReadsSkipSystemStreamsAndContinueAcrossLargeGaps()
        {
            var checkpoint = await TailCheckpoint();
            await WriteSystemEvents(20);
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            await WriteSystemEvents(20);
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(2) }, _timeout.Token);
            await WriteSystemEvents(20);

            var page = await _persistence.LoadAllCommittedEvents(checkpoint, 2, _timeout.Token);
            Assert.That(page.CommittedDomainEvents.Select(e => e.AggregateSequenceNumber), Is.EqualTo(new[] { 1, 2 }));
            Assert.That(page.CommittedDomainEvents.All(e => e.AggregateId == _id.Value), Is.True);
            var empty = await _persistence.LoadAllCommittedEvents(page.NextGlobalPosition, 2, _timeout.Token);
            Assert.That(empty.CommittedDomainEvents, Is.Empty);
            Assert.That(empty.NextGlobalPosition, Is.Not.EqualTo(page.NextGlobalPosition));
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(3) }, _timeout.Token);
            var appended = await _persistence.LoadAllCommittedEvents(empty.NextGlobalPosition, 2, _timeout.Token);
            Assert.That(appended.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(3));
        }

        [Test]
        public async Task GlobalReadsPreserveUnscavengedEventsAfterSoftDeletion()
        {
            var checkpoint = await TailCheckpoint();
            await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            await _persistence.DeleteEventsAsync(_id, _timeout.Token);
            var page = await _persistence.LoadAllCommittedEvents(checkpoint, 10, _timeout.Token);
            Assert.That(page.CommittedDomainEvents.Single().AggregateId, Is.EqualTo(_id.Value));
            Assert.That(await _persistence.LoadCommittedEventsAsync(_id, 1, _timeout.Token), Is.Empty);
        }

        [Test]
        public async Task GlobalReadFromStartIncludesAggregateAndExcludesSystemStreams()
        {
            var serialized = CreateEvent(1);
            await _persistence.CommitEventsAsync(_id, new[] { serialized }, _timeout.Token);
            var page = await _persistence.LoadAllCommittedEvents(GlobalPosition.Start, 10000, _timeout.Token);
            Assert.That(page.CommittedDomainEvents.All(e => !e.AggregateId.StartsWith("$", StringComparison.Ordinal)), Is.True);
            AssertEvent(page.CommittedDomainEvents.Single(e => e.AggregateId == _id.Value), serialized);
        }

        [TestCase("garbage")]
        [TestCase("after:")]
        [TestCase("after:1-2")]
        [TestCase("1-2")]
        [TestCase("-1-0")]
        [TestCase("1--1")]
        [TestCase("after:9223372036854775808-0")]
        [TestCase("unknown:1-0")]
        [TestCase("1-0-0")]
        public void MalformedGlobalCheckpointsAreRejected(string checkpoint)
        {
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _persistence.LoadAllCommittedEvents(new GlobalPosition(checkpoint), 10, _timeout.Token));
        }

        [TestCase(0)]
        [TestCase(-1)]
        public void GlobalReadsRequirePositivePageSize(int pageSize)
        {
            Assert.ThrowsAsync<ArgumentOutOfRangeException>(async () =>
                await _persistence.LoadAllCommittedEvents(GlobalPosition.Start, pageSize, _timeout.Token));
        }

        private async Task<GlobalPosition> TailCheckpoint()
        {
            await foreach (var record in _client.ReadAllAsync(Direction.Backwards, Position.End,
                maxCount: 1, cancellationToken: _timeout.Token))
            {
                var position = record.OriginalPosition.Value;
                return new GlobalPosition(string.Format(System.Globalization.CultureInfo.InvariantCulture,
                    "after:{0}-{1}", position.CommitPosition, position.PreparePosition));
            }
            return GlobalPosition.Start;
        }

        private async Task WriteSystemEvents(int count)
        {
            await _client.AppendToStreamAsync("$eventflow-checkpoint-test-" + Guid.NewGuid().ToString("N"),
                StreamState.NoStream,
                Enumerable.Range(0, count).Select(_ => new EventData(Uuid.NewUuid(), "$test", Encoding.UTF8.GetBytes("{}"))),
                cancellationToken: _timeout.Token);
        }
        [Test]
        public async Task RegisteredPersistenceCanCommitAndReadAcrossScopes()
        {
            var connectionString = TestConnectionStrings.Persistence;
            using (var resolver = EventFlowOptions.New.UseKurrentDBEventStore(connectionString).CreateResolver())
            {
                var serialized = CreateEvent(1);
                using (var writeScope = resolver.BeginScope())
                {
                    await writeScope.Resolve<IEventPersistence>().CommitEventsAsync(
                        _id, new[] { serialized }, _timeout.Token);
                }
                using (var readScope = resolver.BeginScope())
                {
                    var loaded = await readScope.Resolve<IEventPersistence>().LoadCommittedEventsAsync(
                        _id, 1, _timeout.Token);
                    AssertEvent(loaded.Single(), serialized);
                }
            }
        }
        [TestCase(1)]
        [TestCase(2)]
        public async Task LostAppendAcknowledgementDoesNotDuplicateEvents(int sequenceNumber)
        {
            if (sequenceNumber > 1)
                await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);

            var attempts = 0;
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, async (stream, state, events, token) =>
            {
                await _client.AppendToStreamAsync(stream, state, events,
                    configureOperationOptions: options => options.ThrowOnAppendFailure = true, cancellationToken: token);
                if (++attempts == 1)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "acknowledgement lost after commit"));
            });
            var serialized = new[] { CreateEvent(sequenceNumber), CreateEvent(sequenceNumber + 1) };
            var committed = await persistence.CommitEventsAsync(_id, serialized, _timeout.Token);
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(committed.Count, Is.EqualTo(2));
            for (var i = 0; i < serialized.Length; i++) AssertEvent(committed.ElementAt(i), serialized[i]);
            var stored = await ReadRawEvents();
            Assert.That(stored.Length, Is.EqualTo(sequenceNumber + 1));
            Assert.That(stored.Last().Event.Data.ToArray(), Is.EqualTo(Encoding.UTF8.GetBytes(serialized.Last().SerializedData)));
        }

        [Test]
        public async Task TimeoutBeforeServerCommitRetriesTheWrite()
        {
            var attempts = 0;
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, async (stream, state, events, token) =>
            {
                if (++attempts == 1)
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "timeout before commit"));
                await _client.AppendToStreamAsync(stream, state, events,
                    configureOperationOptions: options => options.ThrowOnAppendFailure = true, cancellationToken: token);
            });
            await persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token);
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That((await ReadRawEvents()).Length, Is.EqualTo(1));
        }

        [Test]
        public async Task RivalWriteAfterTimeoutRemainsAConcurrencyConflict()
        {
            var attempts = 0;
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, async (stream, state, events, token) =>
            {
                if (++attempts == 1)
                {
                    // Another command wins the revision before this command retries.
                    await _persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, token);
                    throw new Grpc.Core.RpcException(new Grpc.Core.Status(Grpc.Core.StatusCode.DeadlineExceeded, "timeout before retry"));
                }
                await _client.AppendToStreamAsync(stream, state, events,
                    configureOperationOptions: options => options.ThrowOnAppendFailure = true, cancellationToken: token);
            });
            var exception = Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await persistence.CommitEventsAsync(_id, new[] { CreateEvent(1) }, _timeout.Token));
            Assert.That(exception.InnerException, Is.TypeOf<WrongExpectedVersionException>());
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That((await ReadRawEvents()).Length, Is.EqualTo(1));
        }
        private static SerializedEvent CreateEvent(int sequenceNumber)
        {
            var metadata = new Metadata
            {
                AggregateName = "Thingy",
                EventName = "Ping",
                EventVersion = 2,
                EventId = EventId.New
            };
            return new SerializedEvent("{ \"custom\": \"München 東京 🚚\" }",
                "{ \"message\": \"héllo 東京 🚚\", \"sequence\": " + sequenceNumber + " }",
                sequenceNumber, metadata);
        }

        private void AssertEvent(ICommittedDomainEvent actual, SerializedEvent expected)
        {
            Assert.That(actual.AggregateId, Is.EqualTo(_id.Value));
            Assert.That(actual.AggregateSequenceNumber, Is.EqualTo(expected.AggregateSequenceNumber));
            Assert.That(actual.Data, Is.EqualTo(expected.SerializedData));
            Assert.That(actual.Metadata, Is.EqualTo(expected.SerializedMetadata));
        }

        private async Task<ResolvedEvent[]> ReadRawEvents()
        {
            var events = new System.Collections.Generic.List<ResolvedEvent>();
            await foreach (var record in _client.ReadStreamAsync(Direction.Forwards,
                _id.Value, StreamPosition.Start, cancellationToken: _timeout.Token))
            {
                events.Add(record);
            }
            return events.ToArray();
        }
    }
}
