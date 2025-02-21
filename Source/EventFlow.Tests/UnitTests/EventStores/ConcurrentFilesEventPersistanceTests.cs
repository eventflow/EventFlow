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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Configuration.EventNamingStrategy;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.EventStores.Files;
using EventFlow.Exceptions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.EventStores
{
    [Category(Categories.Unit)]
    public class ConcurrentFilesEventPersistanceTests : Test
    {
        // Higher values have exponential effect on duration
        // due to OptimsticConcurrency and retry
        private const int DegreeOfParallelism = 15;
        private const int NumberOfEventsPerBatch = 10;

        // All threads operate on same thingy
        private static readonly ThingyId ThingyId = ThingyId.New;

        private string _storeRootPath;
        private EventJsonSerializer _serializer;

        [SetUp]
        public void SetUp()
        {
            var factory = new DomainEventFactory();
            var definitionService = new EventDefinitionService(
                Mock<ILogger<EventDefinitionService>>(),
                Mock<ILoadedVersionedTypes>(),
                new NamespaceAndNameStrategy());
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
        [Retry(5)]
        [Ignore("Ignore for now")]
        public void MultipleInstancesWithSamePathFail()
        {
            // Arrange
            var tasks = RunInParallel(async i =>
            {
                var persistence = CreatePersistence("SameForAll");
                await CommitEventsAsync(persistence).ConfigureAwait(false);
            });

            // Act
            Action action = () => Task.WaitAll(tasks.ToArray());

            // Assert
            action.Should().Throw<IOException>("because of concurrent access to the same files.");
        }

        [Test]
        public void MultipleInstancesWithDifferentPathsWork()
        {
            // Arrange
            var tasks = RunInParallel(async i =>
            {
                var persistence = CreatePersistence(i.ToString());
                await CommitEventsAsync(persistence).ConfigureAwait(false);
            });

            // Act
            Action action = () => Task.WaitAll(tasks.ToArray());

            // Assert
            action.Should().NotThrow();
        }

        [Test]
        public void SingleInstanceWorks()
        {
            // Arrange
            var persistence = CreatePersistence();
            var tasks = RunInParallel(async i =>
            {
                await CommitEventsAsync(persistence).ConfigureAwait(false);
            });

            // Act
            Action action = () => Task.WaitAll(tasks.ToArray());

            // Assert
            action.Should().NotThrow();
        }

        private IFilesEventStoreConfiguration ConfigurePath(string storePath)
        {
            var fullPath = Path.Combine(_storeRootPath, storePath);
            return FilesEventStoreConfiguration.Create(fullPath);
        }

        private FilesEventPersistence CreatePersistence(string storePath = "")
        {
            var serializer = new JsonSerializer();
            var config = ConfigurePath(storePath);
            var locator = new FilesEventLocator(config);
            return new FilesEventPersistence(
                Mock<ILogger<FilesEventPersistence>>(),
                serializer,
                config,
                locator);
        }

        private async Task CommitEventsAsync(FilesEventPersistence persistence)
        {
            var events = Enumerable.Range(0, NumberOfEventsPerBatch)
                .Select(i => new ThingyPingEvent(PingId.New))
                .ToList();

            await RetryAsync(async () =>
            {
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
                    .CommitEventsAsync(ThingyId, readOnlyEvents, CancellationToken.None)
                    .ConfigureAwait(false);
            })
            .ConfigureAwait(false);
        }

        private static async Task<int> GetVersionAsync(FilesEventPersistence persistence)
        {
            var existingEvents = await persistence.LoadCommittedEventsAsync(
                ThingyId,
                1,
                CancellationToken.None).ConfigureAwait(false);

            int version = existingEvents.LastOrDefault()?.AggregateSequenceNumber ?? 0;
            return version;
        }

        private static IEnumerable<Task> RunInParallel(Func<int, Task> action)
        {
            var tasks = Enumerable.Range(1, DegreeOfParallelism)
                .AsParallel()
                .WithDegreeOfParallelism(DegreeOfParallelism)
                .Select(action);

            return tasks.ToList();
        }

        private static async Task RetryAsync(Func<Task> action)
        {
            for (int retry = 0; retry < DegreeOfParallelism; retry++)
            {
                try
                {
                    await action().ConfigureAwait(false);
                    return;
                }
                catch (OptimisticConcurrencyException)
                {
                }
            }
        }
    }
}
