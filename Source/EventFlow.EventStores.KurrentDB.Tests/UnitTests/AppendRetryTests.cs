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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using Metadata = EventFlow.Aggregates.Metadata;
using EventFlow.Logs;
using EventFlow.TestHelpers.Aggregates;
using Grpc.Core;
using KurrentDB.Client;
using NUnit.Framework;

namespace EventFlow.EventStores.KurrentDB.Tests.UnitTests
{
    [TestFixture]
    public class AppendRetryTests
    {
        private KurrentDBClient _client;

        [SetUp]
        public void SetUp()
        {
            _client = new KurrentDBClient(KurrentDBClientSettings.Create("kurrentdb://localhost:1?tls=false"));
        }

        [TearDown]
        public void TearDown() => _client.Dispose();

        [Test]
        public async Task TimeoutRetriesKeepTheSameBatchIdsPayloadAndRevision()
        {
            var attempts = 0;
            EventData[] firstBatch = null;
            Uuid[] firstIds = null;
            byte[] firstData = null;
            byte[] firstMetadata = null;
            var id = ThingyId.New;
            using (var cancellation = new CancellationTokenSource())
            {
                var persistence = new KurrentDBEventPersistence(new NullLog(), _client, (stream, state, events, token) =>
                {
                    attempts++;
                    if (firstBatch == null)
                    {
                        firstBatch = events;
                        firstIds = events.Select(e => e.EventId).ToArray();
                        firstData = events[0].Data.ToArray();
                        firstMetadata = events[0].Metadata.ToArray();
                    }
                    Assert.That(stream, Is.EqualTo(id.Value));
                    Assert.That(state, Is.EqualTo(StreamState.NoStream));
                    Assert.That(events, Is.SameAs(firstBatch));
                    Assert.That(events.Select(e => e.EventId), Is.EqualTo(firstIds));
                    Assert.That(events[0].Data.ToArray(), Is.EqualTo(firstData));
                    Assert.That(events[0].Metadata.ToArray(), Is.EqualTo(firstMetadata));
                    Assert.That(token, Is.EqualTo(cancellation.Token));
                    return attempts < 3 ? Task.FromException(Deadline()) : Task.CompletedTask;
                });
                var committed = await persistence.CommitEventsAsync(id, new[] { CreateEvent(1), CreateEvent(2) }, cancellation.Token);
                Assert.That(attempts, Is.EqualTo(3));
                Assert.That(committed.Select(e => e.AggregateSequenceNumber), Is.EqualTo(new[] { 1, 2 }));
            }
        }

        [Test]
        public void ExhaustedTimeoutRetriesPropagateTheLastFailure()
        {
            var attempts = 0;
            RpcException last = null;
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, (stream, state, events, token) =>
            {
                attempts++;
                last = Deadline();
                return Task.FromException(last);
            });
            var exception = Assert.ThrowsAsync<RpcException>(async () =>
                await persistence.CommitEventsAsync(ThingyId.New, new[] { CreateEvent(1) }, CancellationToken.None));
            Assert.That(attempts, Is.EqualTo(5));
            Assert.That(exception, Is.SameAs(last));
        }

        [TestCase(StatusCode.Unavailable)]
        [TestCase(StatusCode.PermissionDenied)]
        [TestCase(StatusCode.Aborted)]
        [TestCase(StatusCode.Cancelled)]
        public void NonDeadlineFailuresAreNotRetried(StatusCode statusCode)
        {
            var attempts = 0;
            var failure = new RpcException(new Status(statusCode, "test"));
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, (stream, state, events, token) =>
            {
                attempts++;
                return Task.FromException(failure);
            });
            var exception = Assert.ThrowsAsync<RpcException>(async () =>
                await persistence.CommitEventsAsync(ThingyId.New, new[] { CreateEvent(1) }, CancellationToken.None));
            Assert.That(attempts, Is.EqualTo(1));
            Assert.That(exception, Is.SameAs(failure));
        }

        [Test]
        public void CancellationDuringBackoffStopsBeforeAnotherAppend()
        {
            var attempts = 0;
            using (var cancellation = new CancellationTokenSource())
            {
                var persistence = new KurrentDBEventPersistence(new NullLog(), _client, (stream, state, events, token) =>
                {
                    attempts++;
                    return Task.FromException(Deadline());
                });
                var commit = persistence.CommitEventsAsync(ThingyId.New, new[] { CreateEvent(1) }, cancellation.Token);
                Assert.That(attempts, Is.EqualTo(1));
                cancellation.Cancel();
                Assert.That(async () => await commit, Throws.InstanceOf<OperationCanceledException>());
                Assert.That(attempts, Is.EqualTo(1));
            }
        }

        [Test]
        public async Task SeparateCommitsDoNotShareStorageIds()
        {
            Uuid? previousId = null;
            var persistence = new KurrentDBEventPersistence(new NullLog(), _client, (stream, state, events, token) =>
            {
                if (previousId.HasValue) Assert.That(events[0].EventId, Is.Not.EqualTo(previousId.Value));
                previousId = events[0].EventId;
                return Task.CompletedTask;
            });
            var id = ThingyId.New;
            var serialized = CreateEvent(1);
            await persistence.CommitEventsAsync(id, new[] { serialized }, CancellationToken.None);
            await persistence.CommitEventsAsync(id, new[] { serialized }, CancellationToken.None);
        }

        private static RpcException Deadline() => new RpcException(new Status(StatusCode.DeadlineExceeded, "simulated append timeout"));

        private static SerializedEvent CreateEvent(int sequence) => new SerializedEvent("{\"metadata\":true}", "{\"data\":true}", sequence,
            new Metadata { AggregateName = "Thingy", EventName = "Ping", EventVersion = 2, EventId = EventId.New });
    }
}