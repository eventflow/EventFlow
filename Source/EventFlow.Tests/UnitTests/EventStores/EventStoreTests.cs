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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventCaches;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using Moq;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.Tests.UnitTests.EventStores
{
    public class EventStoreTests : TestsFor<InMemoryEventStore>
    {
        private Mock<IEventCache> _eventCacheMock;
        private Mock<IEventUpgradeManager> _eventUpgradeManagerMock;
        private Mock<IEventJsonSerializer> _eventJsonSerializerMock;

        [SetUp]
        public void SetUp()
        {
            Fixture.Inject(Enumerable.Empty<IMetadataProvider>());

            _eventJsonSerializerMock = InjectMock<IEventJsonSerializer>();
            _eventCacheMock = InjectMock<IEventCache>();
            _eventUpgradeManagerMock = InjectMock<IEventUpgradeManager>();

            _eventUpgradeManagerMock
                .Setup(m => m.Upgrade<TestAggregate>(It.IsAny<IReadOnlyCollection<IDomainEvent>>()))
                .Returns<IReadOnlyCollection<IDomainEvent>>(c => c);
            _eventJsonSerializerMock
                .Setup(m => m.Serialize(It.IsAny<IAggregateEvent>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns<IAggregateEvent, IEnumerable<KeyValuePair<string, string>>>(
                    (a, m) => new SerializedEvent(
                        string.Empty,
                        string.Empty,
                        int.Parse(m.Single(kv => kv.Key == MetadataKeys.AggregateSequenceNumber).Value)));
        }

        [Test]
        public async Task CacheIsInvalidatedOnStore()
        {
            // Arrange
            var ss = ManyUncommittedEvents(1);

            // Act
            await Sut.StoreAsync<TestAggregate>(A<string>(), ss, CancellationToken.None).ConfigureAwait(false);

            // Assert
            _eventCacheMock.Verify(c => c.InvalidateAsync(It.IsAny<Type>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        private List<IUncommittedEvent> ManyUncommittedEvents(int count = 3)
        {
            return Many<PingEvent>(count)
                .Select((e, i) => (IUncommittedEvent)new UncommittedEvent(e, new Metadata(new Dictionary<string, string>
                    {
                        {MetadataKeys.AggregateSequenceNumber, (i + 1).ToString()}
                    })))
                .ToList();
        }
    }
}
