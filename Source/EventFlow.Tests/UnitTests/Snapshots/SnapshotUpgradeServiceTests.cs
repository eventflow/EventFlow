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

using System.Linq;
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

namespace EventFlow.Tests.UnitTests.Snapshots
{
    [Category(Categories.Unit)]
    public class SnapshotUpgradeServiceTests : TestsFor<SnapshotUpgradeService>
    {
        private Mock<IResolver> _resolverMock;
        private ISnapshotDefinitionService _snapshotDefinitionService;

        [SetUp]
        public void SetUp()
        {
            _resolverMock = InjectMock<IResolver>();
            _snapshotDefinitionService = Inject<ISnapshotDefinitionService>(new SnapshotDefinitionService(A<ILog>()));
        }

        [Test]
        public async Task UpgradeAsync_UpgradesSnapshot()
        {
            // Arrange
            var pingIds = Many<PingId>();
            Arrange_All_Upgraders();
            _snapshotDefinitionService.Load(typeof(ThingySnapshotV1), typeof(ThingySnapshotV2), typeof(ThingySnapshot));

            // Act
            var snapshot = await Sut.UpgradeAsync(new ThingySnapshotV1(pingIds), CancellationToken.None);

            // Assert
            snapshot.Should().BeOfType<ThingySnapshot>();
            var thingySnapshot = (ThingySnapshot) snapshot;
            thingySnapshot.PingsReceived.Should().BeEquivalentTo(pingIds);
            thingySnapshot.PreviousVersions.Should().BeEquivalentTo(new[] {ThingySnapshotVersion.Version1, ThingySnapshotVersion.Version2});
        }

        private void Arrange_All_Upgraders()
        {
            Arrange_Upgraders(
                new ThingySnapshotV1ToV2Upgrader(),
                new ThingySnapshotV2ToV3Upgrader());
        }

        private void Arrange_Upgraders(params object[] snapshotUpgraders)
        {
            foreach (var snapshotUpgrader in snapshotUpgraders)
            {
                var snapshotUpgraderInterfaceType = snapshotUpgrader
                    .GetType()
                    .GetInterfaces()
                    .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISnapshotUpgrader<,>));

                _resolverMock
                    .Setup(r => r.Resolve(snapshotUpgraderInterfaceType))
                    .Returns(snapshotUpgrader);
            }
        }
    }
}