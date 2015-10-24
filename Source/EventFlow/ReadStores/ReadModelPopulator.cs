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
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
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

        public Task PurgeAsync<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : class, IReadModel, new()
        {
            var readModelStores = _resolver.Resolve<IEnumerable<IReadModelStore<TReadModel>>>().ToList();
            if (!readModelStores.Any())
            {
                throw new ArgumentException($"Could not find any read stores for read model '{typeof (TReadModel).PrettyPrint()}'");
            }

            var deleteTasks = readModelStores.Select(s => s.DeleteAllAsync(cancellationToken));
            return Task.WhenAll(deleteTasks);
        }

        public void Purge<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : class, IReadModel, new()
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PurgeAsync<TReadModel>(cancellationToken));
            }
        }

        public async Task PopulateAsync<TReadModel>(
            CancellationToken cancellationToken)
            where TReadModel : class, IReadModel, new()
        {
            var stopwatch = Stopwatch.StartNew();
            var readModelType = typeof (TReadModel);
            var readStoreManagers = ResolveReadStoreManager<TReadModel>();

            var aggregateEventTypes = new HashSet<Type>(readModelType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IAmReadModelFor<,,>))
                .Select(i => i.GetGenericArguments()[2]));

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

        public void Populate<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : class, IReadModel, new()
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PopulateAsync<TReadModel>(cancellationToken));
            }
        }

        private IReadOnlyCollection<IReadStoreManager<TReadModel>> ResolveReadStoreManager<TReadModel>()
            where TReadModel : class, IReadModel, new()
        {
            var readStoreManagers = _resolver.Resolve<IEnumerable<IReadStoreManager>>()
                .Select(m => m as IReadStoreManager<TReadModel>)
                .Where(m => m != null)
                .ToList();

            if (!readStoreManagers.Any())
            {
                throw new ArgumentException($"Did not find any read store managers for read model type '{typeof (TReadModel).PrettyPrint()}'");
            }

            return readStoreManagers;
        }
    }
}
