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
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.Snapshots;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Moq;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Explicit]
    [Category(Categories.Unit)]
    public class ConcurrentInMemoryEventPersistanceTests
    {
        // Higher values have exponential effect on duration
        // due to OptimsticConcurrency and retry
        private const int DegreeOfParallelism = 5;
        private const int NumberOfEvents = 100;

        // All threads operate on same thingy
        private static readonly ThingyId ThingyId = ThingyId.New;

        [Test]
        public async Task MultipleInstances()
        {
            var store = CreateStore();

            // Arrange
            var tasks = RunInParallel(async i =>
            {
                await CommitEventsAsync(store).ConfigureAwait(false);
            });

            // Act
            Task.WaitAll(tasks.ToArray());

            // Assert
            var allEvents = await store.LoadAllEventsAsync(GlobalPosition.Start, Int32.MaxValue, CancellationToken.None);
            allEvents.DomainEvents.Count.Should().Be(NumberOfEvents * DegreeOfParallelism);
        }

        private EventStoreBase CreateStore()
        {
            var aggregateFactory = Mock.Of<IAggregateFactory>();
            var resolver = Mock.Of<IResolver>();
            var metadataProviders = Enumerable.Empty<IMetadataProvider>();
            var snapshotStore = Mock.Of<ISnapshotStore>();
            var log = new NullLog();
            var factory = new DomainEventFactory();
            var persistence = new InMemoryEventPersistence(log);
            var upgradeManager = new EventUpgradeManager(log, resolver);
            var definitionService = new EventDefinitionService(log);
            definitionService.Load(typeof(ThingyPingEvent));
            var serializer = new EventJsonSerializer(new JsonSerializer(), definitionService, factory);

            var store = new EventStoreBase(log, aggregateFactory,
                serializer, upgradeManager, metadataProviders,
                persistence, snapshotStore);

            return store;
        }

        private async Task CommitEventsAsync(EventStoreBase store)
        {
            var events = new[]
            {
                new ThingyPingEvent(PingId.New),
            };

            for (int i = 0; i < NumberOfEvents; i++)
            {
                await RetryAsync(async () =>
                {
                    var allEvents = await store.LoadEventsAsync<ThingyAggregate, ThingyId>(ThingyId,
                        CancellationToken.None).ConfigureAwait(false);

                    var version = allEvents.Count;
                    var serializedEvents
                        = from aggregateEvent in events
                          let metadata = new Metadata
                          {
                              AggregateSequenceNumber = ++version,
                              AggregateName = "ThingyAggregate",
                              Timestamp = DateTimeOffset.Now
                          }
                          let uncommittedEvent = new UncommittedEvent(aggregateEvent, metadata)
                          select uncommittedEvent;

                    var readOnlyEvents = new ReadOnlyCollection<UncommittedEvent>(serializedEvents.ToList());

                    await store.StoreAsync<ThingyAggregate, ThingyId>(ThingyId, readOnlyEvents,
                            SourceId.New, CancellationToken.None)
                            .ConfigureAwait(false);
                }).ConfigureAwait(false);
            }
        }

        private static IEnumerable<Task> RunInParallel(Func<int, Task> action)
        {
            var tasks = Enumerable.Range(1, DegreeOfParallelism)
                .AsParallel()
                .WithDegreeOfParallelism(DegreeOfParallelism)
                .Select(action);

            return tasks.ToList();
        }

        private static readonly Random Random = new Random();

        private static async Task RetryAsync(Func<Task> action)
        {
            for (int retry = 0; retry < 100; retry++)
            {
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (OptimisticConcurrencyException)
                {
                    await Task.Delay(Random.Next(100, 1000));
                }
            }

            throw new Exception("Retried too often.");
        }
    }
}
