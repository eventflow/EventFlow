// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Logging;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using EventFlow.Logging;

namespace EventFlow.Tests.UnitTests.Subscribers
{
    [Category(Categories.Unit)]
    public class DispatchToEventSubscribersTests : TestsFor<DispatchToEventSubscribers>
    {
        private Mock<IResolver> _resolverMock;
        private Mock<IEventFlowConfiguration> _eventFlowConfigurationMock;
        private Mock<ILog> _logMock;

        [SetUp]
        public void SetUp()
        {
            _logMock = InjectMock<ILog>();
            _resolverMock = InjectMock<IResolver>();
            _eventFlowConfigurationMock = InjectMock<IEventFlowConfiguration>();

            Inject<IMemoryCache>(new DictionaryMemoryCache(Mock<ILog>()));
            Inject<ITaskRunner>(new TaskRunner(_logMock.Object));
        }

        [Test]
        public async Task SynchronousSubscribersGetCalled()
        {
            // Arrange
            var subscriberMock = ArrangeSynchronousSubscriber<ThingyPingEvent>();

            // Act
            await Sut.DispatchAsync(new[] { A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>() }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
            _logMock.VerifyNoErrorsLogged();
        }

        [Test]
        public async Task AynchronousSubscribersGetCalled()
        {
            // Arrange
            AutoResetEvent autoResetEvent;
            var subscriberMock = ArrangeAsynchronousSubscriber<ThingyPingEvent>(out autoResetEvent);

            // Act
            await Sut.DispatchAsync(new[] { A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>() }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            autoResetEvent.WaitOne(TimeSpan.FromSeconds(10));
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
            _logMock.VerifyNoErrorsLogged();
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
            Assert.DoesNotThrowAsync(async () => await Sut.DispatchAsync(new[] { A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>() }, CancellationToken.None).ConfigureAwait(false));

            // Assert
            _logMock.Verify(
                m => m.ErrorException(It.IsAny<string>(), expectedException, It.IsAny<object[]>()),
                Times.Once);
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
            var exception = Assert.Throws<Exception>(() => Sut.DispatchAsync(new[] {A<DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>>()}, CancellationToken.None).GetAwaiter().GetResult());

            // Assert
            exception.Should().BeSameAs(expectedException);
            _logMock.VerifyNoErrorsLogged();
        }

        private Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>> ArrangeSynchronousSubscriber<TEvent>()
            where TEvent : IAggregateEvent<ThingyAggregate, ThingyId>
        {
            var subscriberMock = new Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>>();

            _resolverMock
                .Setup(r => r.ResolveAll(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISubscribeSynchronousTo<,,>))))
                .Returns(new object[] { subscriberMock.Object });

            return subscriberMock;
        }

        private Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>> ArrangeAsynchronousSubscriber<TEvent>(out AutoResetEvent autoResetEvent)
            where TEvent : IAggregateEvent<ThingyAggregate, ThingyId>
        {
            var subscriberMock = new Mock<ISubscribeSynchronousTo<ThingyAggregate, ThingyId, TEvent>>();
            var myAutoResetEvent = new AutoResetEvent(false);

            _resolverMock
                .Setup(r => r.ResolveAll(It.Is<Type>(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ISubscribeSynchronousTo<,,>))))
                .Callback(() => myAutoResetEvent.Set())
                .Returns(new object[] { subscriberMock.Object });

            autoResetEvent = myAutoResetEvent;
            return subscriberMock;
        }
    }
}