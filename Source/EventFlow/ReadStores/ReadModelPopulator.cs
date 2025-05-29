// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core.Caching;
using EventFlow.EventStores;
using EventFlow.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.ReadStores
{
    public class ReadModelPopulator : IReadModelPopulator
    {
        private readonly ILogger<ReadModelPopulator> _logger;
        private readonly IEventFlowConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventUpgradeContextFactory _eventUpgradeContextFactory;
        private readonly IMemoryCache _memoryCache;
        private readonly ConcurrentQueue<AllEventsPage> _pipedEvents = new ConcurrentQueue<AllEventsPage>();

        public ReadModelPopulator(
            ILogger<ReadModelPopulator> logger,
            IEventFlowConfiguration configuration,
            IEventStore eventStore,
            IServiceProvider serviceProvider,
            IEventUpgradeContextFactory eventUpgradeContextFactory,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _configuration = configuration;
            _eventStore = eventStore;
            _serviceProvider = serviceProvider;
            _eventUpgradeContextFactory = eventUpgradeContextFactory;
            _memoryCache = memoryCache;
        }

        public Task PurgeAsync<TReadModel>(
            CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            return PurgeAsync(typeof(TReadModel), cancellationToken);
        }

        public async Task PurgeAsync(
            Type readModelType,
            CancellationToken cancellationToken)
        {
            var readModelStores = ResolveReadModelStores(readModelType);

            var deleteTasks = readModelStores.Select(s => s.DeleteAllAsync(cancellationToken));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
        }

        public async Task DeleteAsync(
            string id,
            Type readModelType,
            CancellationToken cancellationToken)
        {
            var readModelStores = ResolveReadModelStores(readModelType);

            _logger.LogTrace(
                "Deleting read model {ReadModelType} with ID {Id}",
                readModelType.PrettyPrint(),
                id);

            var deleteTasks = readModelStores.Select(s => s.DeleteAsync(id, cancellationToken));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
        }

        public Task PopulateAsync<TReadModel>(
            CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            return PopulateAsync(typeof(TReadModel), cancellationToken);
        }

        public Task PopulateAsync(
            Type readModelType,
            CancellationToken cancellationToken)
        {
            return PopulateAsync(new List<Type>() { readModelType }, cancellationToken);
        }

        public async Task PopulateAsync(IReadOnlyCollection<Type> readModelTypes, CancellationToken cancellationToken)
        {
            var combinedReadModelTypeString = string.Join(", ", readModelTypes.Select(type => type.PrettyPrint()));
            _logger.LogInformation("Starting populating of {ReadModelTypes}", combinedReadModelTypeString);

            var loadEventsTasks = LoadEventsAsync(cancellationToken);
            var processEventQueueTask = ProcessEventQueueAsync(readModelTypes, cancellationToken);

            await Task.WhenAll(loadEventsTasks, processEventQueueTask);

            _logger.LogInformation("Population of read models completed");
        }

        private async Task LoadEventsAsync(CancellationToken cancellationToken)
        {
            long totalEvents = 0;
            var currentPosition = GlobalPosition.Start;
            var eventUpgradeContext = await _eventUpgradeContextFactory.CreateAsync(cancellationToken);

            while (true)
            {
                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace(
                        "Loading events starting from {CurrentPosition} and the next {PageSize} for populating",
                        currentPosition,
                        _configuration.LoadReadModelEventPageSize);
                }

                var allEventsPage = await _eventStore.LoadAllEventsAsync(
                    currentPosition,
                    _configuration.LoadReadModelEventPageSize,
                    eventUpgradeContext,
                    cancellationToken)
                    .ConfigureAwait(false);
                totalEvents += allEventsPage.DomainEvents.Count;
                currentPosition = allEventsPage.NextGlobalPosition;

                _pipedEvents.Enqueue(allEventsPage);

                if (!allEventsPage.DomainEvents.Any())
                {
                    _logger.LogTrace(
                        "No more events in event store with a total of {EventTotal} events",
                        totalEvents);
                    break;
                }
            }
        }

        private async Task ProcessEventQueueAsync(
            IReadOnlyCollection<Type> readModelTypes,
            CancellationToken cancellationToken)
        {
            var domainEventsToProcess = new List<IDomainEvent>();

            var hasMoreEvents = true;
            do
            {
                var noEventsToReady = !_pipedEvents.Any();
                if (noEventsToReady)
                {
                    await Task.Delay(100, cancellationToken);
                    continue;
                }

                _pipedEvents.TryDequeue(out var fetchedEvents);
                if (fetchedEvents == null)
                {
                    continue;
                }

                domainEventsToProcess.AddRange(fetchedEvents.DomainEvents);

                hasMoreEvents = fetchedEvents.DomainEvents.Any();
                var batchExceedsThreshold = domainEventsToProcess.Count >= _configuration.PopulateReadModelEventPageSize;
                var processEvents = !hasMoreEvents || batchExceedsThreshold;
                if (processEvents)
                {
                    var readModelUpdateTasks = readModelTypes.Select(readModelType => ProcessEventsAsync(readModelType, domainEventsToProcess, cancellationToken));
                    await Task.WhenAll(readModelUpdateTasks);
                    
                    domainEventsToProcess.Clear();
                }
            }
            while (hasMoreEvents);
        }

        private async Task ProcessEventsAsync(
            Type readModelType,
            IReadOnlyCollection<IDomainEvent> processEvents,
            CancellationToken cancellationToken)
        {
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var readStoreManagers = ResolveReadStoreManagers(readModelType);
                long relevantEvents = 0;

                var readModelTypes = new[]
                {
                    typeof(IAmReadModelFor<,,> )
                };

                var aggregateEventTypes = _memoryCache.GetOrCreate(
                    CacheKey.With(GetType(), readModelType.ToString(), nameof(ProcessEventsAsync)), 
                    _ => new HashSet<Type>(readModelType.GetTypeInfo()
                        .GetInterfaces()
                        .Where(i => i.GetTypeInfo().IsGenericType && readModelTypes.Contains(i.GetGenericTypeDefinition()))
                        .Select(i => i.GetTypeInfo().GetGenericArguments()[2])));

                if (_logger.IsEnabled(LogLevel.Trace))
                {
                    _logger.LogTrace(
                        "Read model {ReadModelType} is interested in these aggregate events: {AggregateEventTypes}",
                        readModelType.PrettyPrint(),
                        aggregateEventTypes.Select(e => e.PrettyPrint()));
                }

                var domainEvents = processEvents
                    .Where(e => aggregateEventTypes.Contains(e.EventType))
                    .ToList();
                relevantEvents += domainEvents.Count;

                if (!domainEvents.Any())
                {
                    _logger.LogWarning($"Will not populate {readModelType.PrettyPrint()} because no events were found");
                    return;
                }

                var applyTasks = readStoreManagers
                    .Select(m => m.UpdateReadStoresAsync(domainEvents, cancellationToken));
                await Task.WhenAll(applyTasks).ConfigureAwait(false);

                _logger.LogInformation(
                    "Population of read model {ReadModelType} took {Seconds} seconds, in which {RelevantEventCount} was relevant",
                    readModelType.PrettyPrint(),
                    stopwatch.Elapsed.TotalSeconds,
                    relevantEvents);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception when populating: {readModelType}");
            }
        }

        private IReadOnlyCollection<IReadModelStore> ResolveReadModelStores(
            Type readModelType)
        {
            var readModelStoreType = typeof(IReadModelStore<>).MakeGenericType(readModelType);
            var readModelStores = _serviceProvider.GetServices(readModelStoreType)
                .Select(s => (IReadModelStore)s)
                .ToList();

            if (!readModelStores.Any())
            {
                throw new ArgumentException($"Could not find any read stores for read model '{readModelType.PrettyPrint()}'");
            }

            return readModelStores;
        }

        private IReadOnlyCollection<IReadStoreManager> ResolveReadStoreManagers(
            Type readModelType)
        {
            return _memoryCache.GetOrCreate(CacheKey.With(GetType(), readModelType.ToString(), nameof(ResolveReadStoreManagers)),
                e => 
                {
                    var readStoreManagers = _serviceProvider.GetServices<IReadStoreManager>()
                    .Where(m => m.ReadModelType == readModelType)
                    .ToList();

                    if (!readStoreManagers.Any())
                    {
                        throw new ArgumentException($"Did not find any read store managers for read model type '{readModelType.PrettyPrint()}'");
                    }

                    return readStoreManagers; 
                });
        }
    }
}
