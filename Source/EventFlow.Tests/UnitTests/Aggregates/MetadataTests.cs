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
using EventFlow.Aggregates;
using EventFlow.TestHelpers;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [Category(Categories.Unit)]
    public class MetadataTests : Test
    {
        [Test]
        public void TimestampIsSerializedCorrectly()
        {
            // Arrange
            var timestamp = A<DateTimeOffset>();

            // Act
            var sut = new Metadata
                {
                    Timestamp = timestamp
                };

            // Assert
            sut.Timestamp.Should().Be(timestamp);
        }

        [Test]
        public void EventNameIsSerializedCorrectly()
        {
            // Arrange
            var eventName = A<string>();

            // Act
            var sut = new Metadata
                {
                    EventName = eventName
                };

            // Assert
            sut.EventName.Should().Be(eventName);
        }

        [Test]
        public void EventVersionIsSerializedCorrectly()
        {
            // Arrange
            var eventVersion = A<int>();

            // Act
            var sut = new Metadata
                {
                    EventVersion = eventVersion
                };

            // Assert
            sut.EventVersion.Should().Be(eventVersion);
        }

        [Test]
        public void AggregateSequenceNumberIsSerializedCorrectly()
        {
            // Arrange
            var aggregateSequenceNumber = A<int>();

            // Act
            var sut = new Metadata
                {
                    AggregateSequenceNumber = aggregateSequenceNumber
                };

            // Assert
            sut.AggregateSequenceNumber.Should().Be(aggregateSequenceNumber);
        }

        [Test]
        public void CloneWithCanMerge()
        {
            // Arrange
            var key1 = A<string>();
            var key2 = A<string>();
            var value1 = A<string>();
            var value2 = A<string>();

            // Act
            var metadata1 = new Metadata { [key1] = value1 };
            var metadata2 = metadata1.CloneWith(new KeyValuePair<string, string>(key2, value2));

            // Assert
            metadata1.ContainsKey(key2).Should().BeFalse();

            metadata2.ContainsKey(key1).Should().BeTrue();
            metadata2.ContainsKey(key2).Should().BeTrue();
            metadata2[key1].Should().Be(value1);
            metadata2[key2].Should().Be(value2);
        }

        [Test]
        public void SerializeDeserializeWithValues()
        {
            // Arrange
            var aggregateName = A<string>();
            var aggregateSequenceNumber = A<int>();
            var timestamp = A<DateTimeOffset>();
            var sut = new Metadata
                {
                    { MetadataKeys.AggregateName, aggregateName },
                    { MetadataKeys.AggregateSequenceNumber, aggregateSequenceNumber.ToString() },
                    { MetadataKeys.Timestamp, timestamp.ToString("O") }
                };

            // Act
            var json = JsonConvert.SerializeObject(sut);
            var metadata = JsonConvert.DeserializeObject<Metadata>(json);

            // Assert
            metadata.Count.Should().Be(3);
            metadata.AggregateName.Should().Be(aggregateName);
            metadata.AggregateSequenceNumber.Should().Be(aggregateSequenceNumber);
            metadata.Timestamp.Should().Be(timestamp);
        }

        [Test]
        public void SerializeDeserializeEmpty()
        {
            // Arrange
            var sut = new Metadata();

            // Act
            var json = JsonConvert.SerializeObject(sut);
            var metadata = JsonConvert.DeserializeObject<Metadata>(json);

            // Assert
            json.Should().Be("{}");
            metadata.Count.Should().Be(0);
        }
    }
}
