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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventCaches.InMemory;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests.EventCaches
{
    public class InMemoryEventCacheTests : TestsFor<InMemoryEventCache>
    {
        [SetUp]
        public void SetUp()
        {
            Fixture.Inject<ILog>(new ConsoleLog());
            Fixture.Inject<ITimeMachine>(new TimeMachine());
        }

        [Test]
        public void InsertNullThrowsException()
        {
            // Act
            Assert.Throws<ArgumentNullException>(
                async () => await Sut.InsertAsync(typeof (TestAggregate), A<string>(), null, CancellationToken.None).ConfigureAwait(false));
        }

        [Test]
        public void EmptyListThrowsException()
        {
            // Act
            Assert.Throws<ArgumentException>(
                async () => await Sut.InsertAsync(typeof(TestAggregate), A<string>(), new List<IDomainEvent>(), CancellationToken.None).ConfigureAwait(false));
        }

        [Test]
        public async Task NoneExistingReturnsNull()
        {
            // Arrange
            var id = A<string>();

            // Act
            var domainEvents = await Sut.GetAsync(typeof(TestAggregate), id, CancellationToken.None).ConfigureAwait(false);

            // Assert
            domainEvents.Should().BeNull();
        }

        [Test]
        public async Task StreamCanBeUpdated()
        {
            // Arrange
            var aggregateType = typeof (TestAggregate);
            var id = A<string>();

            // Act
            await Sut.InsertAsync(aggregateType, id, CreateStream(), CancellationToken.None).ConfigureAwait(false);
            await Sut.InsertAsync(aggregateType, id, CreateStream(), CancellationToken.None).ConfigureAwait(false);
        }

        [Test]
        public async Task InsertAndGetWorks()
        {
            // Arrange
            var aggregateType = typeof(TestAggregate);
            var id = A<string>();
            var domainEvents = CreateStream();

            // Act
            await Sut.InsertAsync(aggregateType, id, domainEvents, CancellationToken.None).ConfigureAwait(false);
            var storedDomainEvents = await Sut.GetAsync(aggregateType, id, CancellationToken.None).ConfigureAwait(false);

            // Assert
            storedDomainEvents.Should().BeSameAs(domainEvents);
        }

        [Test]
        public async Task InvalidateRemoves()
        {
            // Arrange
            var aggregateType = typeof(TestAggregate);
            var id = A<string>();
            var domainEvents = CreateStream();
            
            // Act
            await Sut.InsertAsync(aggregateType, id, domainEvents, CancellationToken.None).ConfigureAwait(false);
            await Sut.InvalidateAsync(aggregateType, id, CancellationToken.None).ConfigureAwait(false);
            var storedEvents = await Sut.GetAsync(aggregateType, id, CancellationToken.None).ConfigureAwait(false);

            // Assert
            storedEvents.Should().BeNull();
        }

        private IReadOnlyCollection<IDomainEvent> CreateStream()
        {
            return Many<DomainEvent<PingEvent>>();
        }
    }
}
