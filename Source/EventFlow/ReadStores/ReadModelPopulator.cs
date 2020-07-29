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
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public class ReadModelPopulator : IReadModelPopulator
    {
        private readonly ILog _log;
        private readonly IEventFlowConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly IResolver _resolver;

        public ReadModelPopulator(
            ILog log,
            IEventFlowConfiguration configuration,
            IEventStore eventStore,
            IResolver resolver)
        {
            _log = log;
            _configuration = configuration;
            _eventStore = eventStore;
            _resolver = resolver;
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

            _log.Verbose(() => $"Deleting read model {readModelType.PrettyPrint()} with ID '{id}'");

            var deleteTasks = readModelStores.Select(s => s.DeleteAsync(id, cancellationToken));
            await Task.WhenAll(deleteTasks).ConfigureAwait(false);
        }

        public Task PopulateAsync<TReadModel>(
            CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            return PopulateAsync(typeof(TReadModel), cancellationToken);
        }

        public async Task PopulateAsync(
            Type readModelType,
            CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();
            var readStoreManagers = ResolveReadStoreManagers(readModelType);

            var readModelTypes = new[]
            {
                typeof( IAmReadModelFor<,,> ),
                typeof( IAmAsyncReadModelFor<,,> )
            };

            var aggregateEventTypes = new HashSet<Type>(readModelType
                .GetTypeInfo()
                .GetInterfaces()
                .Where(i => i.GetTypeInfo().IsGenericType 
                            && readModelTypes.Contains(i.GetGenericTypeDefinition()))
                .Select(i => i.GetTypeInfo().GetGenericArguments()[2]));

            _log.Verbose(() => string.Format(
                "Read model '{0}' is interested in these aggregate events: {1}",
                readModelType.PrettyPrint(),
                string.Join(", ", aggregateEventTypes.Select(e => e.PrettyPrint()).OrderBy(s => s))));

            long totalEvents = 0;
            long relevantEvents = 0;
            var currentPosition = GlobalPosition.Start;

            while (true)
            {
                _log.Verbose(() => string.Format(
                    "Loading events starting from {0} and the next {1} for populating '{2}'",
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    readModelType.PrettyPrint()));
                var allEventsPage = await _eventStore.LoadAllEventsAsync(
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    cancellationToken)
                    .ConfigureAwait(false);
                totalEvents += allEventsPage.DomainEvents.Count;
                currentPosition = allEventsPage.NextGlobalPosition;

                if (!allEventsPage.DomainEvents.Any())
                {
                    _log.Verbose(() => $"No more events in event store, stopping population of read model '{readModelType.PrettyPrint()}'");
                    break;
                }

                var domainEvents = allEventsPage.DomainEvents
                    .Where(e => aggregateEventTypes.Contains(e.EventType))
                    .ToList();
                relevantEvents += domainEvents.Count;

                if (!domainEvents.Any())
                {
                    continue;
                }

                var applyTasks = readStoreManagers
                    .Select(m => m.UpdateReadStoresAsync(domainEvents, cancellationToken));
                await Task.WhenAll(applyTasks).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _log.Information(
                "Population of read model '{0}' took {1:0.###} seconds, in which {2} events was loaded and {3} was relevant",
                readModelType.PrettyPrint(),
                stopwatch.Elapsed.TotalSeconds,
                totalEvents,
                relevantEvents);
        }

        private IReadOnlyCollection<IReadModelStore> ResolveReadModelStores(
            Type readModelType)
        {
            var readModelStoreType = typeof(IReadModelStore<>).MakeGenericType(readModelType);
            var readModelStores = _resolver.ResolveAll(readModelStoreType)
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
            var readStoreManagers = _resolver.Resolve<IEnumerable<IReadStoreManager>>()
                .Where(m => m.ReadModelType == readModelType)
                .ToList();

            if (!readStoreManagers.Any())
            {
                throw new ArgumentException($"Did not find any read store managers for read model type '{readModelType.PrettyPrint()}'");
            }

            return readStoreManagers;
        }
    }
}