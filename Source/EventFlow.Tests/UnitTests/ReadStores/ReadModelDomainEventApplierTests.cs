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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public class ReadModelDomainEventApplierTests : TestsFor<ReadModelDomainEventApplier>
    {
        public class PingReadModel : IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public bool PingEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
            {
                PingEventsReceived = true;
            }
        }

        public class TheOtherPingReadModel : IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public bool PingEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
            {
                PingEventsReceived = true;
            }
        }

        public class AsyncPingReadModel : IReadModel,
            IAmAsyncReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public bool PingEventsReceived { get; private set; }

            public async Task ApplyAsync(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
                CancellationToken cancellationToken)
            {
                await Task.Delay(50, cancellationToken).ConfigureAwait(false);
                PingEventsReceived = true;
            }
        }

        public class DomainErrorAfterFirstReadModel : IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>
        {
            public bool DomainErrorAfterFirstEventsReceived { get; private set; }

            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> domainEvent)
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
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>()),
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
                    ToDomainEvent(A<ThingyPingEvent>()),
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
                    ToDomainEvent(A<ThingyPingEvent>()),
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>()),
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
        public async Task UpdateReturnsFalseIfNoEventsWasApplied()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>()),
                };

            // Act
            var appliedAny = await Sut.UpdateReadModelAsync(
                new PingReadModel(),
                events,
                A<IReadModelContext>(), 
                CancellationToken.None);

            // Assert
            appliedAny.Should().BeFalse();
        }

        [Test]
        public async Task UpdateReturnsTrueIfEventsWereApplied()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>()),
                };

            // Act
            var appliedAny = await Sut.UpdateReadModelAsync(
                new PingReadModel(),
                events,
                A<IReadModelContext>(),
                CancellationToken.None);

            // Assert
            appliedAny.Should().BeTrue();
        }

        [Test]
        public async Task ReadModelReceivesEvent()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>()),
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

        [Test]
        public async Task AsyncReadModelReceivesEvent()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>()),
                };
            var readModel = new AsyncPingReadModel();

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
