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
using EventFlow.Configuration;
using EventFlow.Logs;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using EventFlow.TestHelpers.Aggregates.Snapshots.Upgraders;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores.Snapshots
{
    [Category(Categories.Unit)]
    public class SnapshotUpgradeServiceTests : TestsFor<SnapshotUpgradeService>
    {
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            _resolverMock = InjectMock<IResolver>();
            _resolverMock
                .Setup(r => r.Resolve(typeof(ISnapshotUpgrader<ThingySnapshotV1, ThingySnapshotV2>)))
                .Returns(() => new ThingySnapshotV1ToV2Upgrader());
            _resolverMock
                .Setup(r => r.Resolve(typeof(ISnapshotUpgrader<ThingySnapshotV2, ThingySnapshot>)))
                .Returns(() => new ThingySnapshotV2ToV3Upgrader());

            var snapshotDefinitionService = new SnapshotDefinitionService(Mock<ILog>());
            snapshotDefinitionService.Load(typeof(ThingySnapshotV1), typeof(ThingySnapshotV2), typeof(ThingySnapshot));
            Inject<ISnapshotDefinitionService>(snapshotDefinitionService);
        }

        [Test]
        public async Task UpgradeAsync()
        {
            // Act
            var pingIds = Many<PingId>();
            var snapshot = await Sut.UpgradeAsync(new ThingySnapshotV1(pingIds), CancellationToken.None).ConfigureAwait(false);

            // Assert
            snapshot.Should().BeOfType<ThingySnapshot>();
            var snapshotV3 = (ThingySnapshot) snapshot;
            snapshotV3.PingsReceived.Should().BeEquivalentTo(pingIds);
            snapshotV3.PreviousVersions.Should().Contain(ThingySnapshotVersion.Version1);
            snapshotV3.PreviousVersions.Should().Contain(ThingySnapshotVersion.Version2);
        }
    }
}