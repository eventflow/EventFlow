// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using EventFlow.TestHelpers;
using Moq;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.EventStores;
using EventFlow.Aggregates;
using System;

namespace EventFlow.Tests.UnitTests.ReadStores
{
    [Category(Categories.Unit)]
    public class ReadModelPopulatorTests : BaseReadModelTests<TestReadModel>
    {
        [Test]
        public async Task MultiplePopulateCallsApplyDomainEvents()
        {
            // Arrange
            var extraManager = ArrangeExtraReadModelManager<SecondTestReadModel>();
            ArrangeEventStore(Many<ThingyPingEvent>(6));

            // Act
            await Sut.PopulateAsync(new HashSet<Type> { typeof(TestReadModel), typeof(SecondTestReadModel) }, CancellationToken.None).ConfigureAwait(false);

            // Assert

            // Previous tests unchanged
            _eventStoreMock.Verify(
                s => s.LoadAllEventsAsync(It.IsAny<GlobalPosition>(), It.Is<int>(i => i == ReadModelPageSize), It.IsAny<IEventUpgradeContext>(), It.IsAny<CancellationToken>()),
                Times.Exactly(3)); ;

            _readStoreManagerMock.Verify(
                s => s.UpdateReadStoresAsync(
                    It.Is<IReadOnlyCollection<IDomainEvent>>(l => l.Count == PopulateReadModelPageSize),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(1));

            // New read model received events
            extraManager.Verify(
                s => s.UpdateReadStoresAsync(
                    It.Is<IReadOnlyCollection<IDomainEvent>>(l => l.Count == PopulateReadModelPageSize),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(1));
        }
    }
}
