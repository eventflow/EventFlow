// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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
using EventFlow.Test;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    public class MetadataTests : TestsFor<Metadata>
    {
        [Test]
        public void TimestampIsSerializedCorrectly()
        {
            // Arrange
            var timestamp = A<DateTimeOffset>();

            // Act
            Sut.Timestamp = timestamp;

            // Assert
            Sut.Timestamp.Should().Be(timestamp);
        }

        [Test]
        public void EventNameIsSerializedCorrectly()
        {
            // Arrange
            var eventName = A<string>();

            // Act
            Sut.EventName = eventName;

            // Assert
            Sut.EventName.Should().Be(eventName);
        }

        [Test]
        public void EventVersionIsSerializedCorrectly()
        {
            // Arrange
            var eventVersion = A<int>();

            // Act
            Sut.EventVersion = eventVersion;

            // Assert
            Sut.EventVersion.Should().Be(eventVersion);
        }

        [Test]
        public void AggregateSequenceNumberIsSerializedCorrectly()
        {
            // Arrange
            var aggregateSequenceNumber = A<int>();

            // Act
            Sut.AggregateSequenceNumber = aggregateSequenceNumber;

            // Assert
            Sut.AggregateSequenceNumber.Should().Be(aggregateSequenceNumber);
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
            Sut[key1] = value1;
            var metadata = Sut.CloneWith(new[]
                {
                    new KeyValuePair<string, string>(key2, value2), 
                });

            // Assert
            metadata.ContainsKey(key1).Should().BeTrue();
            metadata.ContainsKey(key2).Should().BeTrue();
            metadata[key1].Should().Be(value1);
            metadata[key2].Should().Be(value2);
        }
    }
}
