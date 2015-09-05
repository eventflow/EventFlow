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
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventSourcing.Events;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    public class ReadModelDomainEventApplierTests : TestsFor<ReadModelDomainEventApplier>
    {
        public class PingReadModel : IReadModel,
            IAmReadModelFor<TestAggregate, TestId, PingEvent>
        {
            public bool PingEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<TestAggregate, TestId, PingEvent> e)
            {
                PingEventsReceived = true;
            }
        }

        public class TheOtherPingReadModel : IReadModel,
            IAmReadModelFor<TestAggregate, TestId, PingEvent>
        {
            public bool PingEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<TestAggregate, TestId, PingEvent> e)
            {
                PingEventsReceived = true;
            }
        }

        public class DomainErrorAfterFirstReadModel : IReadModel,
            IAmReadModelFor<TestAggregate, TestId, DomainErrorAfterFirstEvent>
        {
            public bool DomainErrorAfterFirstEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<TestAggregate, TestId, DomainErrorAfterFirstEvent> e)
            {
                DomainErrorAfterFirstEventsReceived = true;
            }
        }

        [Test]
        public async Task ReadModelDoesNotReceiveOtherEvents()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<DomainErrorAfterFirstEvent>()),
                };
            var readModel = new PingReadModel();

            // Act
            await Sut.UpdateReadModelAsync(readModel, events, A<IReadModelContext>(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            readModel.PingEventsReceived.Should().BeFalse();
        }

        [Test]
        public async Task DifferentReadModelsCanSubscribeToSameEvent()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<PingEvent>()),
                };
            var pingReadModel = new PingReadModel();
            var theOtherPingReadModel = new TheOtherPingReadModel();

            // Act
            await Sut.UpdateReadModelAsync(
                pingReadModel,
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .ConfigureAwait(false);
            await Sut.UpdateReadModelAsync(
                theOtherPingReadModel,
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            pingReadModel.PingEventsReceived.Should().BeTrue();
            theOtherPingReadModel.PingEventsReceived.Should().BeTrue();
        }

        [Test]
        public async Task DifferentReadModelsCanBeUpdated()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<PingEvent>()),
                    ToDomainEvent(A<DomainErrorAfterFirstEvent>()),
                };
            var pingReadModel = new PingReadModel();
            var domainErrorAfterFirstReadModel = new DomainErrorAfterFirstReadModel();

            // Act
            await Sut.UpdateReadModelAsync(
                pingReadModel,
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .ConfigureAwait(false);
            await Sut.UpdateReadModelAsync(
                domainErrorAfterFirstReadModel,
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            pingReadModel.PingEventsReceived.Should().BeTrue();
            domainErrorAfterFirstReadModel.DomainErrorAfterFirstEventsReceived.Should().BeTrue();
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
                new PingReadModel(),
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
                new PingReadModel(),
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .Result;

            // Assert
            appliedAny.Should().BeTrue();
        }

        [Test]
        public async Task ReadModelReceivesEvent()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<PingEvent>()),
                };
            var readModel = new PingReadModel();

            // Act
            await Sut.UpdateReadModelAsync(
                readModel,
                events,
                A<IReadModelContext>(),
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            readModel.PingEventsReceived.Should().BeTrue();
        }
    }
}
