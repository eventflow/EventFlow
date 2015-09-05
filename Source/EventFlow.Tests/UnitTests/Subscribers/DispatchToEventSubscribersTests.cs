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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventSourcing.Events;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Subscribers
{
    public class DispatchToEventSubscribersTests : TestsFor<DispatchToEventSubscribers>
    {
        private Mock<IResolver> _resolverMock;

        [SetUp]
        public void SetUp()
        {
            _resolverMock = InjectMock<IResolver>();
        }

        [Test]
        public async Task SubscribersGetCalled()
        {
            // Arrange
            var subscriberMock = new Mock<ISubscribeSynchronousTo<TestAggregate, TestId, PingEvent>>();
            _resolverMock
                .Setup(r => r.ResolveAll(It.IsAny<Type>()))
                .Returns(new object[] {subscriberMock.Object});

            // Act
            await Sut.DispatchAsync(new[] { A<DomainEvent<TestAggregate, TestId, PingEvent>>() }, CancellationToken.None).ConfigureAwait(false);

            // Assert
            subscriberMock.Verify(s => s.HandleAsync(It.IsAny<IDomainEvent<TestAggregate, TestId, PingEvent>>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
