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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Snapshots;
using FluentAssertions;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Snapshots
{
    [Category(Categories.Unit)]
    public class SnapshotSerilizerTests : TestsFor<SnapshotSerilizer>
    {
        private Mock<ISnapshotUpgradeService> _snapshotUpgradeServiceMock;
        private Mock<ISnapshotDefinitionService> _snapshotDefinitionService;
        private Mock<IJsonSerializer> _jsonSerializer;

        [Test]
        public async Task SerilizeAsync_ReturnsSomething()
        {
            // Arrange
            Arrange_SnapshotDefinition(A<SnapshotDefinition>());
            var snapshotContainer = CreateSnapshotContainer(A<ThingySnapshot>());

            // Act
            var serializedSnapshot = await Sut.SerilizeAsync<ThingyAggregate, ThingyId, ThingySnapshot>(
                snapshotContainer,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            serializedSnapshot.Should().NotBeNull();
            serializedSnapshot.SerializedData.Should().NotBeNullOrEmpty();
            serializedSnapshot.SerializedMetadata.Should().NotBeNullOrEmpty();
        }

        [Test]
        public async Task DeserializeAsync_ReturnsSomething()
        {
            // Arrange
            Arrange_SnapshotDefinition(A<SnapshotDefinition>());
            var thingySnapshotV1 = A<ThingySnapshotV1>();
            var committedSnapshot = CreateCommittedSnapshot(thingySnapshotV1);

            // Act
            var snapshotContainer = await Sut.DeserializeAsync<ThingyAggregate, ThingyId, ThingySnapshot>(
                committedSnapshot,
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            snapshotContainer.Should().NotBeNull();
            snapshotContainer.Metadata.Should().NotBeNull();
            snapshotContainer.Snapshot.Should()
                .NotBeNull()
                .And
                .BeOfType<ThingySnapshot>();
        }

        [SetUp]
        public void SetUp()
        {
            _snapshotUpgradeServiceMock = InjectMock<ISnapshotUpgradeService>();
            _snapshotDefinitionService = InjectMock<ISnapshotDefinitionService>();
            _jsonSerializer = InjectMock<IJsonSerializer>();

            _jsonSerializer
                .Setup(s => s.Serialize(It.IsAny<object>(), It.IsAny<bool>()))
                .Returns<object, bool>((o, b) => JsonConvert.SerializeObject(o));
            _jsonSerializer
                .Setup(s => s.Deserialize<SnapshotMetadata>(It.IsAny<string>()))
                .Returns<string>(JsonConvert.DeserializeObject<SnapshotMetadata>);
            _snapshotUpgradeServiceMock
                .Setup(s => s.UpgradeAsync(It.IsAny<ISnapshot>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(A<ThingySnapshot>());
        }

        private SnapshotDefinition Arrange_SnapshotDefinition(SnapshotDefinition snapshotDefinition)
        {
            _snapshotDefinitionService
                .Setup(s => s.GetDefinition(It.IsAny<Type>()))
                .Returns(snapshotDefinition);
            _snapshotDefinitionService
                .Setup(s => s.GetDefinition(It.IsAny<string>(), It.IsAny<int>()))
                .Returns(snapshotDefinition);
            return snapshotDefinition;
        }

        private CommittedSnapshot CreateCommittedSnapshot(ISnapshot snapshot)
        {
            return new CommittedSnapshot(
                JsonConvert.SerializeObject(new SnapshotMetadata(new Dictionary<string, string>
                    {
                        {SnapshotMetadataKeys.SnapshotName, A<string>()},
                        {SnapshotMetadataKeys.SnapshotVersion, A<int>().ToString()}
                    })),
                JsonConvert.SerializeObject(snapshot));
        }

        private static SnapshotContainer CreateSnapshotContainer(ISnapshot snapshot)
        {
            return new SnapshotContainer(
                snapshot,
                new SnapshotMetadata());
        }
    }
}