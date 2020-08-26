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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Snapshots
{
    [Category(Categories.Unit)]
    public class SnapshotAggregateRootTests : TestsFor<ThingyAggregate>
    {
        private Mock<IEventStore> _eventStoreMock;
        private Mock<ISnapshotStore> _snapshotStore;

        [TestCase(ThingyAggregate.SnapshotEveryVersion - 1, false)]
        [TestCase(ThingyAggregate.SnapshotEveryVersion, true)]
        [TestCase(ThingyAggregate.SnapshotEveryVersion + 1, true)] // Simulate multiple events emitted
        public async Task WillStoreSnapshotsCorrectly(int eventCount, bool expectedToStore)
        {
            // Arrange
            Arrange_Pings(eventCount);

            // Act
            await Sut.CommitAsync(
                _eventStoreMock.Object,
                _snapshotStore.Object,
                A<ISourceId>(),
                CancellationToken.None);

            // Assert
            _snapshotStore.Verify(
                s => s.StoreSnapshotAsync<ThingyAggregate, ThingyId, ThingySnapshot>(It.IsAny<ThingyId>(), It.IsAny<SnapshotContainer>(), It.IsAny<CancellationToken>()),
                expectedToStore ? Times.Once() : Times.Never());
        }

        [TestCase(ThingyAggregate.SnapshotEveryVersion)]
        [TestCase(ThingyAggregate.SnapshotEveryVersion + 1)]
        [TestCase(ThingyAggregate.SnapshotEveryVersion + 2)]
        public async Task SnapshotIsLoaded(int eventsInStore)
        {
            // Arrange
            var thingySnapshot = new ThingySnapshot(
                Many<PingId>(ThingyAggregate.SnapshotEveryVersion),
                Enumerable.Empty<ThingySnapshotVersion>());
            Arrange_Snapshot(thingySnapshot);
            Arrange_EventStore(ManyDomainEvents<ThingyPingEvent>(eventsInStore));

            // Act
            await Sut.LoadAsync(_eventStoreMock.Object, _snapshotStore.Object, CancellationToken.None);

            // Assert
            Sut.Version.Should().Be(eventsInStore);
            Sut.SnapshotVersion.GetValueOrDefault().Should().Be(ThingyAggregate.SnapshotEveryVersion);
        }

        [SetUp]
        public void SetUp()
        {
            _eventStoreMock = InjectMock<IEventStore>();
            _snapshotStore = InjectMock<ISnapshotStore>();
        }

        [Description("Mock test")]
        [TestCase(5, 3, 3)]
        [TestCase(5, 0, 5)]
        [TestCase(0, 1, 0)]
        public async Task Test_Arrange_EventStore(int eventInStore, int fromEventSequenceNumber, int expectedNumberOfEvents)
        {
            // Arrange
            Arrange_EventStore(ManyDomainEvents<ThingyPingEvent>(eventInStore));

            // Act
            var domainEvents = await _eventStoreMock.Object.LoadEventsAsync<ThingyAggregate, ThingyId>(
                A<ThingyId>(),
                fromEventSequenceNumber,
                CancellationToken.None);

            // Assert
            domainEvents.Should().HaveCount(expectedNumberOfEvents);
        }

        private void Arrange_EventStore(IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> domainEvents)
        {
            var domainEventList = domainEvents.ToList();

            _eventStoreMock
                .Setup(e => e.LoadEventsAsync<ThingyAggregate, ThingyId>(It.IsAny<ThingyId>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Returns<ThingyId, int, CancellationToken>((id, seq, c) => Task.FromResult<IReadOnlyCollection<IDomainEvent<ThingyAggregate, ThingyId>>>(domainEventList.Skip(Math.Max(seq - 1, 0)).ToList()));
        }

        private void Arrange_Snapshot(ThingySnapshot thingySnapshot)
        {
            _snapshotStore
                .Setup(s => s.LoadSnapshotAsync<ThingyAggregate, ThingyId, ThingySnapshot>(It.IsAny<ThingyId>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new SnapshotContainer(
                        thingySnapshot,
                        new SnapshotMetadata
                            {
                                AggregateSequenceNumber = thingySnapshot.PingsReceived.Count
                            }));
        }

        private IReadOnlyCollection<PingId> Arrange_Pings(int count = 3)
        {
            var pingIds = Many<PingId>(count);
            foreach (var pingId in pingIds)
            {
                Sut.Ping(pingId);
            }
            return pingIds;
        }
    }
}