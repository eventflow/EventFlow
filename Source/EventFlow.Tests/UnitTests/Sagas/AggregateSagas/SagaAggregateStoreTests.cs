// The MIT License (MIT)
//
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Sagas;
using EventFlow.Sagas.AggregateSagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Sagas;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Sagas.AggregateSagas
{
    [Category(Categories.Unit)]
    public class SagaAggregateStoreTests : TestsFor<SagaAggregateStore>
    {
        [SetUp]
        public void SetUp()
        {
            Inject<IMemoryCache>(A<MemoryCache>());
        }

        [Test]
        public async Task AggregateStore_UpdateAsync_IsInvoked()
        {
            // Arrange
            var aggregateStoreMock = InjectMock<IAggregateStore>();
            var thingySagaId = A<ThingySagaId>();
            var sourceId = A<SourceId>();
            aggregateStoreMock
                .Setup(s => s.UpdateAsync(
                    thingySagaId,
                    sourceId,
                    It.IsAny<Func<ThingySaga, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new List<IDomainEvent>());

            // Act
            await Sut.UpdateAsync(
                thingySagaId,
                typeof(ThingySaga),
                sourceId,
                (s, c) => Task.FromResult(0),
                CancellationToken.None);

            // Assert
            aggregateStoreMock.Verify(
                s => s.UpdateAsync(
                    thingySagaId,
                    sourceId,
                    It.IsAny<Func<ThingySaga, CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}