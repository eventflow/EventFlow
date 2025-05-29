// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Subscribers
{
    [Category(Categories.Unit)]
    public class DispatchToEventSubscribersTests : TestsFor<DispatchToEventSubscribers>
    {
        private Mock<IServiceProvider> _serviceProviderMock;
        private Mock<IEventFlowConfiguration> _eventFlowConfigurationMock;
        private LoggerMock<DispatchToEventSubscribers> _logMock;

        [SetUp]
        public void SetUp()
        {
            _logMock = new LoggerMock<DispatchToEventSubscribers>();
            _serviceProviderMock = InjectMock<IServiceProvider>();
            _eventFlowConfigurationMock = InjectMock<IEventFlowConfiguration>();
        }

        [Test]
        public async Task SynchronousSubscribersGetCalled()
        {
            // Arrange
            var subscriberMock = ArrangeSynchronousSubscriber<ThingyPingEvent>();

            // Act
            await Sut.DispatchToSynchronousSubscribersAsync(new[] { A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>() }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
            _logMock.VerifyNoProblems();
        }

        [Test]
        public async Task AsynchronousSubscribersGetCalled()
        {
            // Arrange
            var subscriberMock = ArrangeAsynchronousSubscriber<ThingyPingEvent>();

            // Act
            await Sut.DispatchToAsynchronousSubscribersAsync(A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
            _logMock.VerifyNoProblems();
        }

        [Test]
        [Repeat(500)]
        public void SubscriberExceptionIsNotThrownIfNotConfigured()
        {
            // Arrange
            var subscriberMock = ArrangeSynchronousSubscriber<ThingyPingEvent>();
            var expectedException = A<Exception>();
            _eventFlowConfigurationMock
                .Setup(c => c.ThrowSubscriberExceptions)
                .Returns(false);
            subscriberMock
                .Setup(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            // Act
            Assert.DoesNotThrowAsync(async () => await Sut.DispatchToSynchronousSubscribersAsync(new[] { A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>() }, CancellationToken.None).ConfigureAwait(false));

            // Assert
            _logMock.VerifyProblemLogged(expectedException);
        }

        [Test]
        public void SubscriberExceptionIsThrownIfConfigured()
        {
            // Arrange
            var subscriberMock = ArrangeSynchronousSubscriber<ThingyPingEvent>();
            var expectedException = A<Exception>();
            _eventFlowConfigurationMock
                .Setup(c => c.ThrowSubscriberExceptions)
                .Returns(true);
            subscriberMock
                .Setup(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()))
                .Throws(expectedException);

            // Act
            var exception = Assert.Throws<AggregateException>(() => Sut.DispatchToSynchronousSubscribersAsync(new[] {A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>()}, CancellationToken.None).GetAwaiter().GetResult());

            // Assert
            exception.InnerException.Should().BeSameAs(expectedException);
            _logMock.VerifyNoProblems();
        }

        [Test]
        public async Task OnlyOneAsynchronousSubscriberGetCalled()
        {
            // Arrange
            var subscriberMock = ArrangeMultipleAsynchronousSubscribers<ThingyPingEvent>();

            // Act
            await Sut.DispatchToAsynchronousSubscribersAsync(A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
            _logMock.VerifyNoProblems();
        }

        private Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>> ArrangeSynchronousSubscriber<TEvent>()
            where TEvent : IAggregateEvent<ThingyAggregate, ThingyId>
        {
            var subscriberMock = new Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>>();


            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>>)))
                .Returns(new object[] { subscriberMock.Object });

            return subscriberMock;
        }

        private Mock<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>> ArrangeAsynchronousSubscriber<TEvent>()
            where TEvent : IAggregateEvent<ThingyAggregate, ThingyId>
        {
            var subscriberMock = new Mock<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>>();

            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>>)))
                .Returns(new object[] { subscriberMock.Object });

            return subscriberMock;
        }

        private Mock<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>> ArrangeMultipleAsynchronousSubscribers<TEvent>()
            where TEvent : IAggregateEvent<ThingyAggregate, ThingyId>
        {
            var subscriberMock = new Mock<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>>();

            _serviceProviderMock
                .Setup(r => r.GetService(typeof(IEnumerable<ISubscribeAsynchronousTo<ThingyAggregate, ThingyId, TEvent>>)))
                .Returns(new object[] { subscriberMock.Object, subscriberMock.Object, subscriberMock.Object });

            return subscriberMock;
        }
    }
}
