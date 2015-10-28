// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
// 
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [TestFixture]
    public class AggregateRootTests : TestsFor<TestAggregate>
    {
        [Test]
        public void InitialVersionIsZero()
        {
            // Assert
            Sut.Version.Should().Be(0);
            Sut.IsNew.Should().BeTrue();
            Sut.UncommittedEvents.Count().Should().Be(0);
        }

        [Test]
        public void ApplyingEventIncrementsVersion()
        {
            // Act
            Sut.Ping(PingId.New);

            // Assert
            Sut.Version.Should().Be(1);
            Sut.IsNew.Should().BeFalse();
            Sut.UncommittedEvents.Count().Should().Be(1);
            Sut.PingsReceived.Count.Should().Be(1);
        }

        [Test]
        public void EventsCanBeApplied()
        {
            // Arrange
            var events = Many<PingEvent>(2);

            // Act
            Sut.ApplyEvents(events);

            // Assert
            Sut.IsNew.Should().BeFalse();
            Sut.Version.Should().Be(2);
            Sut.PingsReceived.Count.Should().Be(2);
            Sut.UncommittedEvents.Count().Should().Be(0);
        }

        [Test]
        public void EmptyListCanBeApplied()
        {
            // Act
            Sut.ApplyEvents(new IDomainEvent[]{});

            // Assert
            Sut.Version.Should().Be(0);
        }

        [Test]
        public void ApplyIsInvoked()
        {
            // Act
            Sut.DomainErrorAfterFirst();

            // Assert
            Sut.DomainErrorAfterFirstReceived.Should().BeTrue();
        }

        [Test]
        public void ApplyEventsReadsAggregateSequenceNumber()
        {
            // Arrange
            const int expectedVersion = 7;
            var domainEvent = ToDomainEvent(A<PingEvent>(), expectedVersion);

            // Act
            Sut.ApplyEvents(new []{ domainEvent });

            // Assert
            Sut.Version.Should().Be(expectedVersion);
        }
    }
}
