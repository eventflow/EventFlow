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

using System.Threading;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    public class ReadModelFactoryTest : TestsFor<ReadModelFactory>
    {
        public class FancyReadModel : IReadModel,
            IAmReadModelFor<TestAggregate, TestId, PingEvent>
        {
            public int PingEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<TestAggregate, TestId, PingEvent> e)
            {
                PingEventsReceived++;
            }
        }

        [Test]
        public void ReadModelDoesNotReceiveOtherEvents()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<DomainErrorAfterFirstEvent>()),
                };

            // Act
            var readModel = Sut.CreateReadModelAsync<FancyReadModel>(events, A<IReadModelContext>(), CancellationToken.None).Result;

            // Assert
            readModel.PingEventsReceived.Should().Be(0);
        }

        [Test]
        public void UpdateReturnsFalseIfNoEventsWasApplied()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<DomainErrorAfterFirstEvent>()),
                };

            // Act
            var appliedAny = Sut.UpdateReadModelAsync(
                new FancyReadModel(),
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .Result;

            // Assert
            appliedAny.Should().BeFalse();
        }

        [Test]
        public void UpdateReturnsTrueIfEventsWereApplied()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<PingEvent>()),
                };

            // Act
            var appliedAny = Sut.UpdateReadModelAsync(
                new FancyReadModel(),
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .Result;

            // Assert
            appliedAny.Should().BeTrue();
        }

        [Test]
        public void ReadModelReceivesEvent()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<PingEvent>()),
                };

            // Act
            var readModel = Sut.CreateReadModelAsync<FancyReadModel>(events, A<IReadModelContext>(), CancellationToken.None).Result;

            // Assert
            readModel.PingEventsReceived.Should().Be(1);
        }
    }
}
