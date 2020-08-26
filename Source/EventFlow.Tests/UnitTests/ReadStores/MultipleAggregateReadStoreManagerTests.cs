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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public class MultipleAggregateReadStoreManagerTests : ReadStoreManagerTestSuite<MultipleAggregateReadStoreManager<IReadModelStore<TestReadModel>, TestReadModel, IReadModelLocator>>
    {
        private Mock<IReadModelLocator> _readModelLocator;

        [SetUp]
        public void SetUp()
        {
            _readModelLocator = InjectMock<IReadModelLocator>();

            _readModelLocator.Setup(l => l.GetReadModelIds(It.IsAny<IDomainEvent>())).Returns(new[] {A<string>()});
        }

        [Test]
        public async Task LocatorShouldNotBeInvokedForIrelevantDomainEvents()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>())
                };

            // Act
            await Sut.UpdateReadStoresAsync(events, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readModelLocator.Verify(l => l.GetReadModelIds(It.IsAny<IDomainEvent>()), Times.Never);
        }

        [Test]
        public async Task LocatorShouldOnlyBeInvokedForIrelevantDomainEvents()
        {
            // Arrange
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>()),
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>())
                };

            // Act
            await Sut.UpdateReadStoresAsync(events, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readModelLocator.Verify(l => l.GetReadModelIds(It.IsAny<IDomainEvent>()), Times.Once);
        }

        [Test]
        public async Task IfNoReadModelIdsAreReturned_ThenDontInvokeTheReadModelStore()
        {
            // Arrange
            _readModelLocator.Setup(l => l.GetReadModelIds(It.IsAny<IDomainEvent>())).Returns(Enumerable.Empty<string>());
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>()),
                };

            // Act
            await Sut.UpdateReadStoresAsync(events, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _readModelLocator.Verify(l => l.GetReadModelIds(It.IsAny<IDomainEvent>()), Times.Once);
            ReadModelStoreMock.Verify(
                s => s.UpdateAsync(
                    It.IsAny<IReadOnlyCollection<ReadModelUpdate>>(),
                    It.IsAny<IReadModelContextFactory>(),
                    It.IsAny<Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TestReadModel>, CancellationToken, Task<ReadModelUpdateResult<TestReadModel>>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}