// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Category(Categories.Unit)]
    public class EventUpgradeManagerTests : TestsFor<EventUpgradeManager>
    {
        private ServiceProvider _serviceProvider;

        [SetUp]
        public void SetUp()
        {
            _serviceProvider = new ServiceCollection()
                .AddSingleton<IEventUpgrader<ThingyAggregate, ThingyId>>(new UpgradeTestEventV1ToTestEventV2(DomainEventFactory))
                .AddSingleton<IEventUpgrader<ThingyAggregate, ThingyId>>(new UpgradeTestEventV2ToTestEventV3(DomainEventFactory))
                .AddSingleton<IEventUpgrader<ThingyAggregate, ThingyId>>(new DamagedEventRemover())
                .BuildServiceProvider();

            Inject<IServiceProvider>(_serviceProvider);
        }

        [Test]
        public async Task EmptyListReturnsEmptyList()
        {
            // Arrange
            var events = new IDomainEvent<ThingyAggregate, ThingyId>[] { };

            // Act
            var upgradedEvents = await Sut.UpgradeAsync(events.ToAsyncEnumerable(), CancellationToken.None).ToArrayAsync();

            // Assert
            upgradedEvents.Should().BeEmpty();
        }

        [Test]
        public async Task EventWithNoUpgradersIsReturned()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(new ThingyPingEvent(PingId.New)),
                    ToDomainEvent(new ThingyDomainErrorAfterFirstEvent())
                };

            // Act
            var upgradedEvents = await Sut.UpgradeAsync(events.ToAsyncEnumerable(), CancellationToken.None).ToArrayAsync();

            // Assert
            upgradedEvents.Length.Should().Be(2);
            upgradedEvents.Should().Contain(events);
        }

        [Test]
        public async Task EventsAreUpgradedToLatestVersion()
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
            var upgradedEvents = await Sut.UpgradeAsync(events.ToAsyncEnumerable(), CancellationToken.None).ToArrayAsync();

            // Assert
            upgradedEvents.Length.Should().Be(3);
            foreach (var upgradedEvent in upgradedEvents)
            {
                upgradedEvent.Should().BeAssignableTo<IDomainEvent<ThingyAggregate, ThingyId, TestEventV3>>();
            }
        }

        public class TestEventV1 : AggregateEvent<ThingyAggregate, ThingyId> { }
        public class TestEventV2 : AggregateEvent<ThingyAggregate, ThingyId> { }
        public class TestEventV3 : AggregateEvent<ThingyAggregate, ThingyId> { }
        public class DamagedEvent : AggregateEvent<ThingyAggregate, ThingyId> { }

        public class UpgradeTestEventV1ToTestEventV2 : EventUpgraderNonAsync<ThingyAggregate, ThingyId>
        {
            private readonly IDomainEventFactory _domainEventFactory;

            public UpgradeTestEventV1ToTestEventV2(IDomainEventFactory domainEventFactory)
            {
                _domainEventFactory = domainEventFactory;
            }

            protected override IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> Upgrade(
                IDomainEvent<ThingyAggregate, ThingyId> domainEvent)
            {
                yield return !(domainEvent is IDomainEvent<ThingyAggregate, ThingyId, TestEventV1> _)
                    ? domainEvent
                    : _domainEventFactory.Upgrade<ThingyAggregate, ThingyId>(domainEvent, new TestEventV2());
            }
        }

        public class UpgradeTestEventV2ToTestEventV3 : EventUpgraderNonAsync<ThingyAggregate, ThingyId>
        {
            private readonly IDomainEventFactory _domainEventFactory;

            public UpgradeTestEventV2ToTestEventV3(IDomainEventFactory domainEventFactory)
            {
                _domainEventFactory = domainEventFactory;
            }

            protected override IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> Upgrade(
                IDomainEvent<ThingyAggregate, ThingyId> domainEvent)
            {
                yield return !(domainEvent is IDomainEvent<ThingyAggregate, ThingyId, TestEventV2> _)
                    ? domainEvent
                    : _domainEventFactory.Upgrade<ThingyAggregate, ThingyId>(domainEvent, new TestEventV3());
            }
        }

        public class DamagedEventRemover : EventUpgraderNonAsync<ThingyAggregate, ThingyId>
        {
            protected override IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> Upgrade(
                IDomainEvent<ThingyAggregate, ThingyId> domainEvent)
            {
                if (!(domainEvent is IDomainEvent<ThingyAggregate, ThingyId, DamagedEvent> _))
                {
                    yield return domainEvent;
                }
            }
        }
    }
}
