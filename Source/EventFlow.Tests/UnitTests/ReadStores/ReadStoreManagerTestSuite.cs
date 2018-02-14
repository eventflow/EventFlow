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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    public abstract class ReadStoreManagerTestSuite<T> : TestsFor<T>
        where T : IReadStoreManager<ReadStoreManagerTestReadModel>
    {
        protected Mock<IReadModelStore<ReadStoreManagerTestReadModel>> ReadModelStoreMock { get; private set; }
        protected Mock<IReadModelDomainEventApplier> ReadModelDomainEventApplierMock { get; private set; }
        protected IReadOnlyCollection<IDomainEvent> AppliedDomainEvents { get; private set; }

        [SetUp]
        public void SetUpReadStoreManagerTestSuite()
        {
            Inject<ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy>>(
                new TransientFaultHandler<IOptimisticConcurrencyRetryStrategy>(
                    Mock<ILog>(),
                    new OptimisticConcurrencyRetryStrategy(new EventFlowConfiguration())));

            ReadModelStoreMock = InjectMock<IReadModelStore<ReadStoreManagerTestReadModel>>();

            ReadModelDomainEventApplierMock = InjectMock<IReadModelDomainEventApplier>();
            ReadModelDomainEventApplierMock
                .Setup(m => m.UpdateReadModelAsync(
                    It.IsAny<ReadStoreManagerTestReadModel>(),
                    It.IsAny<IReadOnlyCollection<IDomainEvent>>(),
                    It.IsAny<IReadModelContext>(),
                    It.IsAny<CancellationToken>()))
                .Callback<
                    ReadStoreManagerTestReadModel,
                    IReadOnlyCollection<IDomainEvent>,
                    IReadModelContext,
                    CancellationToken>((rm, d, c, _) => AppliedDomainEvents = d);
            AppliedDomainEvents = new IDomainEvent[] {};
        }

        [Test]
        public async Task ReadStoreIsUpdatedWithRelevantEvents()
        {
            // Arrange
            Arrange_ReadModelStore_UpdateAsync(ReadModelEnvelope<ReadStoreManagerTestReadModel>.With(
                A<string>(),
                A<ReadStoreManagerTestReadModel>(),
                0));
            var events = new[]
                {
                    ToDomainEvent(A<ThingyPingEvent>(), 1),
                    ToDomainEvent(A<ThingyDomainErrorAfterFirstEvent>(), 2)
                };

            // Act
            await Sut.UpdateReadStoresAsync(events, CancellationToken.None).ConfigureAwait(false);

            // Assert
            ReadModelStoreMock.Verify(
                s => s.UpdateAsync(
                    It.Is<IReadOnlyCollection<ReadModelUpdate>>(l => l.Count == 1),
                    It.IsAny<IReadModelContext>(),
                    It.IsAny<Func<
                        IReadModelContext,
                        IReadOnlyCollection<IDomainEvent>,
                        ReadModelEnvelope<ReadStoreManagerTestReadModel>,
                        CancellationToken,
                        Task<ReadModelEnvelope<ReadStoreManagerTestReadModel>>>>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        protected void Arrange_ReadModelStore_UpdateAsync(params ReadModelEnvelope<ReadStoreManagerTestReadModel>[] readModelEnvelopes)
        {
            // Don't try this at home...

            ReadModelStoreMock
                .Setup(m => m.UpdateAsync(
                    It.IsAny<IReadOnlyCollection<ReadModelUpdate>>(),
                    It.IsAny<IReadModelContext>(),
                    It.IsAny<Func<
                        IReadModelContext,
                        IReadOnlyCollection<IDomainEvent>,
                        ReadModelEnvelope<ReadStoreManagerTestReadModel>,
                        CancellationToken,
                        Task<ReadModelEnvelope<ReadStoreManagerTestReadModel>>>>(),
                    It.IsAny<CancellationToken>()))
                .Callback<
                    IReadOnlyCollection<ReadModelUpdate>,
                    IReadModelContext,
                    Func<
                        IReadModelContext,
                        IReadOnlyCollection<IDomainEvent>,
                        ReadModelEnvelope<ReadStoreManagerTestReadModel>,
                        CancellationToken,
                        Task<ReadModelEnvelope<ReadStoreManagerTestReadModel>>>,
                        CancellationToken>((readModelUpdates, readModelContext, updaterFunc, cancellationToken) =>
                            {
                                foreach (var g in readModelEnvelopes.GroupBy(e => e.ReadModelId))
                                {
                                    foreach (var readModelEnvelope in g)
                                    {
                                        updaterFunc(
                                            readModelContext,
                                            readModelUpdates
                                                .Where(d => d.ReadModelId == g.Key)
                                                .SelectMany(d => d.DomainEvents)
                                                .OrderBy(d => d.AggregateSequenceNumber)
                                                .ToList(),
                                            readModelEnvelope,
                                            cancellationToken);
                                    }
                                }
                            })
                .Returns(Task.FromResult(0));
        }
    }
}