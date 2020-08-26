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
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Aggregates
{
    [TestFixture]
    [Category(Categories.Unit)]
    public class AggregateRootTests : TestsFor<ThingyAggregate>
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
            var events = Many<ThingyPingEvent>(2);
            var domainEvents = events
                .Select((e, i) => new DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>(
                    e, Metadata.Empty,
                    DateTimeOffset.UtcNow, ThingyId.New, i + 1))
                .ToArray();

            // Act
            Sut.ApplyEvents(domainEvents);

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
        public void ApplyIsInvokedForExplicitImplementations()
        {
            // Act
            Sut.Delete();

            // Assert
            Sut.IsDeleted.Should().BeTrue();
        }

        [Test]
        public void UncommittedEventIdsShouldBeDistinct()
        {
            // Act
            Sut.Ping(A<PingId>());
            Sut.Ping(A<PingId>());

            // Assert
            Sut.UncommittedEvents
                .Select(e => e.Metadata.EventId).Distinct()
                .Should().HaveCount(2);
        }

        [Test]
        public void UncommittedEventIdsShouldBeDeterministic()
        {
            // Arrange
            Inject(ThingyId.With("thingy-75e925aa-9b01-4615-89ee-2a2ecf91d7e8"));

            // Act
            Sut.Ping(A<PingId>());
            Sut.Ping(A<PingId>());

            // Assert
            var eventIdGuids = Sut.UncommittedEvents
                .Select(e => e.Metadata.EventId.Value)
                .ToList();

            // GuidFactories.Deterministic.Namespaces.Events, $"{thingyId.Value}-v1"
            eventIdGuids[0]
                .Should()
                .Be("event-3dde5ccb-b594-59b4-ad0a-4d432ffce026");
            // GuidFactories.Deterministic.Namespaces.Events, $"{thingyId.Value}-v2"
            eventIdGuids[1]
                .Should()
                .Be("event-2e79868f-6ef7-5c88-a941-12ae7ae801c7");
        }

        [Test]
        public void ApplyEventWithOutOfOrderSequenceNumberShouldThrow()
        {
            // Arrange
            const int expectedVersion = 7;
            var domainEvent = ToDomainEvent(A<ThingyPingEvent>(), expectedVersion);

            // Act
            Action applyingEvents = () => Sut.ApplyEvents(new []{ domainEvent });

            // Assert
            applyingEvents.Should().Throw<InvalidOperationException>();
        }
    }
}