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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Snapshots;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.TestHelpers.Suites
{
    public abstract class TestSuiteForSnapshotStore : IntegrationTest
    {
        [Test]
        public async Task GetSnapshotAsync_NoneExistingSnapshotReturnsNull()
        {
            // Act
            var committedSnapshot = await SnapshotPersistence.GetSnapshotAsync(
                typeof(ThingyAggregate),
                ThingyId.New,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            committedSnapshot.Should().BeNull();
        }

        [Test]
        public void DeleteSnapshotAsync_GetSnapshotAsync_NoneExistingSnapshotDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrowAsync(async () => await SnapshotPersistence.DeleteSnapshotAsync(
                typeof(ThingyAggregate),
                ThingyId.New,
                CancellationToken.None));
        }

        [Test]
        public void PurgeSnapshotsAsync_NoneExistingSnapshotDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrowAsync(async () => await SnapshotPersistence.PurgeSnapshotsAsync(typeof(ThingyAggregate), CancellationToken.None));
        }

        [Test]
        public void PurgeSnapshotsAsync_EmptySnapshotStoreDoesNotThrow()
        {
            // Act + Assert
            Assert.DoesNotThrowAsync(async () => await SnapshotPersistence.PurgeSnapshotsAsync(CancellationToken.None));
        }

        [Test]
        public async Task NoSnapshotsAreCreatedWhenCommittingFewEvents()
        {
            // Arrange
            var thingyId = ThingyId.New;
            await PublishPingCommandsAsync(thingyId, ThingyAggregate.SnapshotEveryVersion - 1).ConfigureAwait(false);

            // Act
            var thingySnapshot = await LoadSnapshotAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingySnapshot.Should().BeNull();
        }

        [Test]
        public async Task SnapshotIsCreatedWhenCommittingManyEvents()
        {
            // Arrange
            var thingyId = ThingyId.New;
            const int pingsSent = ThingyAggregate.SnapshotEveryVersion + 1;
            await PublishPingCommandsAsync(thingyId, pingsSent).ConfigureAwait(false);

            // Act
            var thingySnapshot = await LoadSnapshotAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingySnapshot.Should().NotBeNull();
            thingySnapshot.PingsReceived.Count.Should().Be(ThingyAggregate.SnapshotEveryVersion);
        }

        [Test]
        public async Task LoadedAggregateHasCorrectVersionsWhenSnapshotIsApplied()
        {
            // Arrange
            var thingyId = ThingyId.New;
            const int pingsSent = ThingyAggregate.SnapshotEveryVersion + 1;
            await PublishPingCommandsAsync(thingyId, pingsSent).ConfigureAwait(false);

            // Act
            var thingyAggregate = await LoadAggregateAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingyAggregate.Version.Should().Be(pingsSent);
            thingyAggregate.SnapshotVersion.GetValueOrDefault().Should().Be(ThingyAggregate.SnapshotEveryVersion);
        }

        [Test]
        public async Task LoadingNoneExistingSnapshottedAggregateReturnsVersionZeroAndNull()
        {
            // Act
            var thingyAggregate = await LoadAggregateAsync(A<ThingyId>()).ConfigureAwait(false);

            // Assert
            thingyAggregate.Should().NotBeNull();
            thingyAggregate.Version.Should().Be(0);
            thingyAggregate.SnapshotVersion.Should().NotHaveValue();
        }

        [Test]
        public async Task OldSnapshotsAreUpgradedToLatestVersionAndHaveCorrectMetadata()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var pingIds = Many<PingId>();
            var expectedVersion = pingIds.Count;
            var thingySnapshotV1 = new ThingySnapshotV1(pingIds);
            await StoreSnapshotAsync(thingyId, expectedVersion, thingySnapshotV1).ConfigureAwait(false);

            // Act
            var snapshotContainer = await SnapshotStore.LoadSnapshotAsync<ThingyAggregate, ThingyId, ThingySnapshot>(
                thingyId,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            snapshotContainer.Snapshot.Should().BeOfType<ThingySnapshot>();
            snapshotContainer.Metadata.AggregateId.Should().Be(thingyId.Value);
            snapshotContainer.Metadata.AggregateName.Should().Be("ThingyAggregate");
            snapshotContainer.Metadata.AggregateSequenceNumber.Should().Be(expectedVersion);
            snapshotContainer.Metadata.SnapshotName.Should().Be("thingy");
            snapshotContainer.Metadata.SnapshotVersion.Should().Be(1);
        }

        [Test]
        public async Task TheSameSnapshotVersionCanBeSavedMultipleTimes()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var aggregateSequenceNumber = A<int>();

            // Act
            await StoreSnapshotAsync(thingyId, aggregateSequenceNumber, A<ThingySnapshot>()).ConfigureAwait(false);
            await StoreSnapshotAsync(thingyId, aggregateSequenceNumber, A<ThingySnapshot>()).ConfigureAwait(false);
            await StoreSnapshotAsync(thingyId, aggregateSequenceNumber, A<ThingySnapshot>()).ConfigureAwait(false);
        }

        [Test]
        public async Task SnapshotsCanBeSavedOutOfOrder()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            const int aggregateSequenceNumberHigh = 84;
            const int aggregateSequenceNumberLow = 42;

            // Act
            await StoreSnapshotAsync(thingyId, aggregateSequenceNumberHigh, A<ThingySnapshot>()).ConfigureAwait(false);
            await StoreSnapshotAsync(thingyId, aggregateSequenceNumberLow, A<ThingySnapshot>()).ConfigureAwait(false);
        }

        [Test]
        public async Task OldSnapshotsAreUpgradedToLatestVersionAndAppliedToAggregate()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var pingIds = Many<PingId>();
            var expectedVersion = pingIds.Count;
            var thingySnapshotV1 = new ThingySnapshotV1(pingIds);
            await StoreSnapshotAsync(thingyId, expectedVersion, thingySnapshotV1).ConfigureAwait(false);

            // Act
            var thingyAggregate = await LoadAggregateAsync(thingyId).ConfigureAwait(false);

            // Assert
            thingyAggregate.Version.Should().Be(expectedVersion);
            thingyAggregate.PingsReceived.Should().BeEquivalentTo(pingIds);
            thingyAggregate.SnapshotVersions.Should().Contain(new[] {ThingySnapshotVersion.Version1, ThingySnapshotVersion.Version2});
        }

        public Task StoreSnapshotAsync<TSnapshot>(
            ThingyId thingyId,
            int aggregateSequenceNumber,
            TSnapshot snapshot)
            where TSnapshot : ISnapshot
        {
            var snapshotDefinition = SnapshotDefinitionService.GetDefinition(typeof(TSnapshot));
            var snapshotMetadata = new SnapshotMetadata
                {
                    AggregateId = thingyId.Value,
                    AggregateName = "ThingyAggregate",
                    AggregateSequenceNumber = aggregateSequenceNumber,
                    SnapshotName = snapshotDefinition.Name,
                    SnapshotVersion = snapshotDefinition.Version,
                };

            return SnapshotPersistence.SetSnapshotAsync(
                typeof(ThingyAggregate),
                thingyId,
                new SerializedSnapshot(
                    JsonConvert.SerializeObject(snapshotMetadata),
                    JsonConvert.SerializeObject(snapshot),
                    snapshotMetadata),
                CancellationToken.None);
        }

        protected async Task<ThingySnapshot> LoadSnapshotAsync(ThingyId thingyId)
        {
            var snapshotContainer = await SnapshotStore.LoadSnapshotAsync<ThingyAggregate, ThingyId, ThingySnapshot>(
                thingyId,
                CancellationToken.None)
                .ConfigureAwait(false);
            return (ThingySnapshot)snapshotContainer?.Snapshot;
        }
    }
}