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
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.MsSql;
using EventFlow.TestHelpers.Suites;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests.EventStores
{
    [Category(Categories.Integration)]
    public class MsSqlEventStoreTests : TestSuiteForEventStore
    {
        private IMsSqlDatabase _testDatabase;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow");

            var resolver = eventFlowOptions
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .UseEventStore<MsSqlEventPersistence>()
                .CreateResolver();

            var databaseMigrator = resolver.Resolve<IMsSqlDatabaseMigrator>();
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            databaseMigrator.MigrateDatabaseUsingEmbeddedScripts(GetType().Assembly);

            return resolver;
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.Dispose();
        }

        [TestCase(5, 1)]
        [TestCase(5, 3)]
        [TestCase(42, 10)]
        [TestCase(10, 1000)]
        public async Task ImportEvents(int numberOfBatches, int batchSize)
        {
            // Arrange
            Configuration.StreamingBatchSize = batchSize;
            var thingyId = ThingyId.New;
            var batches = Enumerable.Range(0, numberOfBatches)
                .Select(batchNum => (IReadOnlyCollection<IUncommittedEvent>) Enumerable.Range(1, batchSize)
                    .Select(seqNum => CreateUncommittedEvent(thingyId.Value, batchNum * batchSize + seqNum))
                    .ToList())
                .ToList();

            // Act
            await EventStore.ImportEventsAsync(
                AsyncEnumerable.CreateEnumerable(() => new Batchs(batches)),
                CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            var thingyAggregate = await AggregateStore.LoadAsync<ThingyAggregate, ThingyId>(
                thingyId,
                CancellationToken.None)
                .ConfigureAwait(false);
            thingyAggregate.Version.Should().Be(numberOfBatches * batchSize);
        }


        private class Batchs : IAsyncEnumerator<IReadOnlyCollection<IUncommittedEvent>>
        {
            private List<IReadOnlyCollection<IUncommittedEvent>>.Enumerator _uncommittedEventsStream;

            public IReadOnlyCollection<IUncommittedEvent> Current => _uncommittedEventsStream.Current;

            public Batchs(
                List<IReadOnlyCollection<IUncommittedEvent>> uncommittedEventsStream)
            {
                _uncommittedEventsStream = uncommittedEventsStream.GetEnumerator();
            }

            public Task<bool> MoveNext(CancellationToken cancellationToken)
            {
                return Task.FromResult(_uncommittedEventsStream.MoveNext());
            }

            public void Dispose()
            {
            }
        }

        private static IUncommittedEvent CreateUncommittedEvent(string id, int sequenceNumber)
        {
            var aggregateEvent = new ThingyPingEvent(PingId.New);
            var metadata = new Metadata
            {
                AggregateId = id,
                AggregateName = "ThingyAggregate",
                AggregateSequenceNumber = sequenceNumber,
                EventId = EventId.New,
                EventName = "ThingyPing",
                EventVersion = 1,
                SourceId = new SourceId(Guid.NewGuid().ToString("D")),
                Timestamp = DateTimeOffset.Now,
                [MetadataKeys.BatchId] = Guid.NewGuid().ToString(),
            };
            return new UncommittedEvent(
                aggregateEvent,
                metadata);
        }
    }
}