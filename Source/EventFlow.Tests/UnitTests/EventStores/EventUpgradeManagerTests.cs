﻿// The MIT License (MIT)
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

using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventSourcing;
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    public class EventUpgradeManagerTests : TestsFor<EventUpgradeManager>
    {
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            _resolverMock = InjectMock<IResolver>();

            _resolverMock
                .Setup(r => r.Resolve<IEnumerable<IEventUpgrader<TestAggregate, TestId>>>())
                .Returns(new IEventUpgrader<TestAggregate, TestId>[]
                    {
                        new UpgradeTestEventV1ToTestEventV2(DomainEventFactory),
                        new UpgradeTestEventV2ToTestEventV3(DomainEventFactory), 
                        new DamagedEventRemover(),
                    });
            _resolverMock
                .Setup(r => r.ResolveAll(typeof(IEventUpgrader<TestAggregate, TestId>)))
                .Returns(new object[]
                    {
                        new UpgradeTestEventV1ToTestEventV2(DomainEventFactory),
                        new UpgradeTestEventV2ToTestEventV3(DomainEventFactory), 
                        new DamagedEventRemover(),
                    });
        }

        [Test]
        public void EmptyListReturnsEmptyList()
        {
            // Arrange
            var events = new IDomainEvent<TestAggregate, TestId>[] { };

            // Act
            var upgradedEvents = Sut.Upgrade(events);

            // Assert
            upgradedEvents.Should().BeEmpty();
        }

        [Test]
        public void EventWithNoUpgradersIsReturned()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(new PingEvent(PingId.New)),
                    ToDomainEvent(new DomainErrorAfterFirstEvent())
                };

            // Act
            var upgradedEvents = Sut.Upgrade(events);

            // Assert
            upgradedEvents.Count.Should().Be(2);
            upgradedEvents.Should().Contain(events);
        }

        [Test]
        public void EventsAreUpgradedToLatestVersion()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(new TestEventV1()),
                    ToDomainEvent(new TestEventV2()),
                    ToDomainEvent(new DamagedEvent()),
                    ToDomainEvent(new TestEventV3()),
                };

            // Act
            var upgradedEvents = Sut.Upgrade(events);

            // Assert
            upgradedEvents.Count.Should().Be(3);
            foreach (var upgradedEvent in upgradedEvents)
            {
                upgradedEvent.Should().BeAssignableTo<IDomainEvent<TestAggregate, TestId, TestEventV3>>();
            }
        }

        public class TestEventV1 : Event<TestAggregate, TestId> { }
        public class TestEventV2 : Event<TestAggregate, TestId> { }
        public class TestEventV3 : Event<TestAggregate, TestId> { }
        public class DamagedEvent : Event<TestAggregate, TestId> { }

        public class UpgradeTestEventV1ToTestEventV2 : IEventUpgrader<TestAggregate, TestId>
        {
            private readonly IDomainEventFactory _domainEventFactory;

            public UpgradeTestEventV1ToTestEventV2(IDomainEventFactory domainEventFactory)
            {
                _domainEventFactory = domainEventFactory;
            }

            public IEnumerable<IDomainEvent<TestAggregate, TestId>> Upgrade(IDomainEvent<TestAggregate, TestId> domainEvent)
            {
                var testEvent1 = domainEvent as IDomainEvent<TestAggregate, TestId, TestEventV1>;
                yield return testEvent1 == null
                    ? domainEvent
                    : _domainEventFactory.Upgrade<TestAggregate, TestId>(domainEvent, new TestEventV2());
            }
        }

        public class UpgradeTestEventV2ToTestEventV3 : IEventUpgrader<TestAggregate, TestId>
        {
            private readonly IDomainEventFactory _domainEventFactory;

            public UpgradeTestEventV2ToTestEventV3(IDomainEventFactory domainEventFactory)
            {
                _domainEventFactory = domainEventFactory;
            }

            public IEnumerable<IDomainEvent<TestAggregate, TestId>> Upgrade(IDomainEvent<TestAggregate, TestId> domainEvent)
            {
                var testEvent2 = domainEvent as IDomainEvent<TestAggregate, TestId, TestEventV2>;
                yield return testEvent2 == null
                    ? domainEvent
                    : _domainEventFactory.Upgrade<TestAggregate, TestId>(domainEvent, new TestEventV3());
            }
        }

        public class DamagedEventRemover : IEventUpgrader<TestAggregate, TestId>
        {
            public IEnumerable<IDomainEvent<TestAggregate, TestId>> Upgrade(IDomainEvent<TestAggregate, TestId> domainEvent)
            {
                var damagedEvent = domainEvent as IDomainEvent<TestAggregate, TestId, DamagedEvent>;
                if (damagedEvent == null)
                {
                    yield return domainEvent;
                }
            }
        }
    }
}
