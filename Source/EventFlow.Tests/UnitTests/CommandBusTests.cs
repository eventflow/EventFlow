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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Test;
using EventFlow.Test.Aggregates.Test;
using EventFlow.Test.Aggregates.Test.Commands;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests
{
    [TestFixture]
    public class CommandBusTests : TestsFor<CommandBus>
    {
        private Mock<IEventStore> _eventStoreMock;

        [SetUp]
        public void SetUp()
        {
            Fixture.Inject<IEventFlowConfiguration>(new EventFlowConfiguration());
            _eventStoreMock = Freze<IEventStore>();
        }

        [Test]
        public void RetryForOptimisticConcurrencyExceptionsAreDone()
        {
            _eventStoreMock
                .Setup(s => s.LoadAggregateAsync<TestAggregate>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(new TestAggregate("42")));
            _eventStoreMock
                .Setup(s => s.StoreAsync<TestAggregate>(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<CancellationToken>()))
                .Throws(new OptimisticConcurrencyException(string.Empty, null));

            Assert.Throws<OptimisticConcurrencyException>(async () => await Sut.PublishAsync(new DomainErrorAfterFirstCommand("42"), CancellationToken.None).ConfigureAwait(false));

            _eventStoreMock.Verify(
                s => s.StoreAsync<TestAggregate>(It.IsAny<string>(), It.IsAny<IReadOnlyCollection<IUncommittedEvent>>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3));
        }
    }
}
