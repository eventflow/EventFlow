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
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.EventStores.Files;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Category(Categories.Unit)]
    public class FilesEventPersistenceTests
    {
        private const int NumberOfAggregates = 1;
        private const int NumberOfEventsPerAggregate = 3;
        private static readonly ThingyId ThingyId = ThingyId.New;
    
        private string _storeRootPath;
        private EventJsonSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            var factory = new DomainEventFactory();
            var definitionService = new EventDefinitionService(new NullLog());
            definitionService.Load(typeof(ThingyPingEvent));

            _serializer = new EventJsonSerializer(new JsonSerializer(), definitionService, factory);
        }

        [SetUp]
        public void CreateStoreRootDir()
        {
            _storeRootPath = Path.Combine(
                Path.GetTempPath(),
                Guid.NewGuid().ToString());

            Directory.CreateDirectory(_storeRootPath);
        }

        [TearDown]
        public void DeleteStoreRootDir()
        {
            Directory.Delete(_storeRootPath, true);
        }

        [Test]
        public async Task EnumerateAllAggregatePaths()
        {
            // Arrange
            var persistence = CreatePersistence();
            await CommitEventsAsync(persistence).ConfigureAwait(false);

            // Act
            var events = await persistence.LoadAllCommittedEvents(GlobalPosition.Start, int.MaxValue, CancellationToken.None);

            // Assert
            events.CommittedDomainEvents.Count.Should().Be(NumberOfAggregates * NumberOfEventsPerAggregate);
        }

        private FilesEventPersistence CreatePersistence()
        {
            var log = new NullLog();
            var serializer = new JsonSerializer();
            var config = FilesEventStoreConfiguration.Create(_storeRootPath);
            var locator = new FilesEventLocator(config);
            return new FilesEventPersistence(log, serializer, config, locator);
        }

        private async Task CommitEventsAsync(IEventPersistence persistence)
        {
            var events = Enumerable.Range(0, NumberOfEventsPerAggregate)
                .Select(i => new ThingyPingEvent(PingId.New))
                .ToList();

            var version = await GetVersionAsync(persistence).ConfigureAwait(false);

            var serializedEvents = from aggregateEvent in events
                let metadata = new Metadata
                {
                    AggregateSequenceNumber = ++version
                }
                let serializedEvent = _serializer.Serialize(aggregateEvent, metadata)
                select serializedEvent;

            var readOnlyEvents = new ReadOnlyCollection<SerializedEvent>(serializedEvents.ToList());

            await persistence
                .CommitEventsAsync(typeof(ThingyAggregate), ThingyId, readOnlyEvents, CancellationToken.None)
                .ConfigureAwait(false);
        }

        private static async Task<int> GetVersionAsync(IEventPersistence persistence)
        {
            var existingEvents = await persistence.LoadCommittedEventsAsync(
                typeof(ThingyAggregate),
                ThingyId,
                1,
                CancellationToken.None).ConfigureAwait(false);

            var version = existingEvents.LastOrDefault()?.AggregateSequenceNumber ?? 0;
            return version;
        }
    }
}