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

using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Snapshots
{
    [Category(Categories.Unit)]
    public class SnapshotMetadataTests : Test
    {
        [Test]
        public void DeserializesCorrectly()
        {
            // Arrange
            var json = new
                {
                    aggregate_id = "thingy-42",
                    aggregate_name = "thingy",
                    aggregate_sequence_number = "42",
                    snapshot_name = "thingy",
                    snapshot_version = "84",
                }.ToJson();

            // Act
            var snapshotMetadata = JsonConvert.DeserializeObject<SnapshotMetadata>(json);

            // Assert
            snapshotMetadata.AggregateId.Should().Be("thingy-42");
            snapshotMetadata.AggregateName.Should().Be("thingy");
            snapshotMetadata.AggregateSequenceNumber.Should().Be(42);
            snapshotMetadata.SnapshotName.Should().Be("thingy");
            snapshotMetadata.SnapshotVersion.Should().Be(84);
        }

        [Test]
        public void GettersAndSettersWork()
        {
            // Arrange
            var snapshotMetadata = new SnapshotMetadata
                {
                    AggregateId = "thingy-42",
                    AggregateName = "thingy",
                    AggregateSequenceNumber = 42,
                    SnapshotName = "thingy",
                    SnapshotVersion = 84,
                };

            // Act
            var json = JsonConvert.SerializeObject(snapshotMetadata);
            var deserializedSnapshotMetadata = JsonConvert.DeserializeObject<SnapshotMetadata>(json);

            // Assert
            deserializedSnapshotMetadata.AggregateId.Should().Be("thingy-42");
            deserializedSnapshotMetadata.AggregateName.Should().Be("thingy");
            deserializedSnapshotMetadata.AggregateSequenceNumber.Should().Be(42);
            deserializedSnapshotMetadata.SnapshotName.Should().Be("thingy");
            deserializedSnapshotMetadata.SnapshotVersion.Should().Be(84);
        }
    }
}