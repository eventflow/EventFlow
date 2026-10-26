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
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.EventStore;
using EventFlow.EventStores.KurrentDB.Extensions;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using KurrentDB.Client;
using Newtonsoft.Json;
using NUnit.Framework;
using Tcp = EventStore.ClientAPI;

namespace EventFlow.EventStores.KurrentDB.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Categories.Integration)]
    [NonParallelizable]
    public class TcpMigrationTests
    {
        private Tcp.IEventStoreConnection _connection;
        private KurrentDBClient _client;
        private EventStoreEventPersistence _tcp;
        private KurrentDBEventPersistence _grpc;
        private ThingyId _id;
        private CancellationTokenSource _timeout;
        private string _grpcConnectionString;

        [SetUp]
        public async Task SetUp()
        {
            _id = ThingyId.New;
            _timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var settings = Tcp.ConnectionSettings.Create()
                .LimitReconnectionsTo(0)
                .LimitRetriesForOperationTo(0)
                .SetOperationTimeoutTo(TimeSpan.FromSeconds(10))
#if NET48
                .SetDefaultUserCredentials(new Tcp.SystemData.UserCredentials("admin", "changeit"))
#endif
                .Build();
            var tcpUri = new Uri(TestConnectionStrings.MigrationTcp);
            _connection = Tcp.EventStoreConnection.Create(settings, tcpUri);
            await _connection.ConnectAsync();
            _tcp = new EventStoreEventPersistence(new NullLog(), _connection);
            _grpcConnectionString = TestConnectionStrings.Migration;
            _client = new KurrentDBClient(KurrentDBClientSettings.Create(_grpcConnectionString));
            _grpc = new KurrentDBEventPersistence(new NullLog(), _client);
        }

        [TearDown]
        public void TearDown()
        {
            _client?.Dispose();
            _connection?.Dispose();
            _timeout?.Dispose();
        }

        [Test]
        public async Task TcpWrittenAggregateCanBeReplayedAndExtendedThroughGrpc()
        {
            var pingIds = Enumerable.Range(0, 3).Select(_ => PingId.New).ToArray();
            var serialized = pingIds.Select((pingId, index) => CreateEvent(index + 1, pingId)).ToArray();
            await _tcp.CommitEventsAsync(_id, serialized, _timeout.Token);

            var loaded = await _grpc.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            AssertEventsEqual(loaded, serialized);

            using (var resolver = EventFlowOptions.New
                .RegisterServices(services => services.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .AddEvents(typeof(ThingyPingEvent))
                .UseKurrentDBEventStore(_grpcConnectionString)
                .CreateResolver())
            {
                var aggregate = await resolver.Resolve<IAggregateStore>()
                    .LoadAsync<ThingyAggregate, ThingyId>(_id, _timeout.Token);
                Assert.That(aggregate.Version, Is.EqualTo(3));
                Assert.That(aggregate.PingsReceived, Is.EqualTo(pingIds));
                var nextPing = PingId.New;
                aggregate.Ping(nextPing);
                await aggregate.CommitAsync(resolver.Resolve<IEventStore>(), resolver.Resolve<ISnapshotStore>(),
                    SourceId.New, _timeout.Token);
                var reloaded = await resolver.Resolve<IAggregateStore>()
                    .LoadAsync<ThingyAggregate, ThingyId>(_id, _timeout.Token);
                Assert.That(reloaded.Version, Is.EqualTo(4));
                Assert.That(reloaded.PingsReceived, Is.EqualTo(pingIds.Concat(new[] { nextPing })));
            }

            var readBackThroughTcp = await _tcp.LoadCommittedEventsAsync(_id, 1, _timeout.Token);
            Assert.That(readBackThroughTcp.Select(e => e.AggregateSequenceNumber), Is.EqualTo(new[] { 1, 2, 3, 4 }));
            AssertEventsEqual(readBackThroughTcp.Take(3).ToArray(), serialized);
        }

        [Test]
        public async Task TcpCheckpointResumesWithoutRepeatingOrSkippingEvents()
        {
            var start = await TcpTailCheckpoint();
            var serialized = Enumerable.Range(1, 3).Select(n => CreateEvent(n, PingId.New)).ToArray();
            await _tcp.CommitEventsAsync(_id, serialized, _timeout.Token);
            var first = await _tcp.LoadAllCommittedEvents(start, 1, _timeout.Token);
            Assert.That(first.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(1));
            Assert.That(first.NextGlobalPosition.Value, Does.Not.StartWith("after:"));

            var second = await _grpc.LoadAllCommittedEvents(first.NextGlobalPosition, 1, _timeout.Token);
            Assert.That(second.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(2));
            Assert.That(second.NextGlobalPosition.Value, Does.StartWith("after:"));
            var third = await _grpc.LoadAllCommittedEvents(second.NextGlobalPosition, 1, _timeout.Token);
            Assert.That(third.CommittedDomainEvents.Single().AggregateSequenceNumber, Is.EqualTo(3));
            AssertEventsEqual(first.CommittedDomainEvents.Concat(second.CommittedDomainEvents)
                .Concat(third.CommittedDomainEvents).ToArray(), serialized);
        }

        [Test]
        public async Task TcpCheckpointAtEndOfLogSeesLaterAppends()
        {
            await _tcp.CommitEventsAsync(_id, new[] { CreateEvent(1, PingId.New) }, _timeout.Token);
            var checkpoint = await TcpTailCheckpoint();
            var empty = await _grpc.LoadAllCommittedEvents(checkpoint, 10, _timeout.Token);
            Assert.That(empty.CommittedDomainEvents, Is.Empty);
            var next = CreateEvent(2, PingId.New);
            await _tcp.CommitEventsAsync(_id, new[] { next }, _timeout.Token);
            var appended = await _grpc.LoadAllCommittedEvents(empty.NextGlobalPosition, 10, _timeout.Token);
            AssertEventsEqual(appended.CommittedDomainEvents, new[] { next });
        }

        [Test]
        public async Task ExplicitTcpTransactionCanBePagedByCommitAndPreparePosition()
        {
            var start = await TcpTailCheckpoint();
            var serialized = Enumerable.Range(1, 3).Select(n => CreateEvent(n, PingId.New)).ToArray();
            using (var transaction = await _connection.StartTransactionAsync(_id.Value, Tcp.ExpectedVersion.NoStream))
            {
                await transaction.WriteAsync(serialized.Select(e => new Tcp.EventData(
                    Guid.NewGuid(), "Thingy.ThingyPing.1", true,
                    Encoding.UTF8.GetBytes(e.SerializedData), Encoding.UTF8.GetBytes(e.SerializedMetadata))));
                await transaction.CommitAsync();
            }

            var first = await _tcp.LoadAllCommittedEvents(start, 1, _timeout.Token);
            var second = await _grpc.LoadAllCommittedEvents(first.NextGlobalPosition, 1, _timeout.Token);
            var third = await _grpc.LoadAllCommittedEvents(second.NextGlobalPosition, 1, _timeout.Token);
            AssertEventsEqual(first.CommittedDomainEvents.Concat(second.CommittedDomainEvents)
                .Concat(third.CommittedDomainEvents).ToArray(), serialized);

            // TCP stream reads omit global positions; $all carries both offsets.
            var parts = start.Value.Split('-');
            var raw = await _connection.ReadAllEventsForwardAsync(new Tcp.Position(
                long.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
                long.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)), 4096, false);
            var positions = raw.Events.Where(e => e.OriginalStreamId == _id.Value)
                .Select(e => e.OriginalPosition.Value).ToArray();
            Assert.That(positions.Select(p => p.CommitPosition).Distinct().Count(), Is.EqualTo(1));
            Assert.That(positions.Select(p => p.PreparePosition).Distinct().Count(), Is.EqualTo(3));
        }

        [Test]
        public async Task StaleTcpWriterConflictsAfterGrpcAppend()
        {
            await _tcp.CommitEventsAsync(_id, new[] { CreateEvent(1, PingId.New) }, _timeout.Token);
            await _grpc.CommitEventsAsync(_id, new[] { CreateEvent(2, PingId.New) }, _timeout.Token);
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await _tcp.CommitEventsAsync(_id, new[] { CreateEvent(2, PingId.New) }, _timeout.Token));
            Assert.ThrowsAsync<OptimisticConcurrencyException>(async () =>
                await _grpc.CommitEventsAsync(_id, new[] { CreateEvent(1, PingId.New) }, _timeout.Token));
        }

        private async Task<GlobalPosition> TcpTailCheckpoint()
        {
            var page = await _tcp.LoadAllCommittedEvents(GlobalPosition.Start, 4096, _timeout.Token);
            return page.NextGlobalPosition;
        }

        private SerializedEvent CreateEvent(int sequenceNumber, PingId pingId)
        {
            var metadata = new Metadata
            {
                AggregateName = "Thingy",
                AggregateId = _id.Value,
                AggregateSequenceNumber = sequenceNumber,
                EventName = "ThingyPing",
                EventVersion = 1,
                EventId = EventId.New,
                SourceId = SourceId.New,
                Timestamp = DateTimeOffset.UtcNow,
                ["custom"] = "München 東京 🚚"
            };
            return new SerializedEvent(JsonConvert.SerializeObject(metadata),
                JsonConvert.SerializeObject(new ThingyPingEvent(pingId)), sequenceNumber, metadata);
        }

        private void AssertEventsEqual(IReadOnlyCollection<ICommittedDomainEvent> actual, SerializedEvent[] expected)
        {
            Assert.That(actual.Count, Is.EqualTo(expected.Length));
            var records = actual.ToArray();
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.That(records[i].AggregateId, Is.EqualTo(_id.Value));
                Assert.That(records[i].AggregateSequenceNumber, Is.EqualTo(expected[i].AggregateSequenceNumber));
                Assert.That(records[i].Data, Is.EqualTo(expected[i].SerializedData));
                Assert.That(records[i].Metadata, Is.EqualTo(expected[i].SerializedMetadata));
            }
        }
    }
}
