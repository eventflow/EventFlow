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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;

namespace EventFlow.ReadStores
{
    public class ReadModelPopulator : IReadModelPopulator
    {
        public Task PurgeAsync<TReadModel>(CancellationToken cancellationToken) where TReadModel : IReadModel
        {
            throw new NotImplementedException();
        }

        public void Purge<TReadModel>(CancellationToken cancellationToken) where TReadModel : IReadModel
        {
            throw new NotImplementedException();
        }

        public Task PopulateAsync<TReadModel>(CancellationToken cancellationToken) where TReadModel : IReadModel
        {
            throw new NotImplementedException();
        }

        public void Populate<TReadModel>(CancellationToken cancellationToken) where TReadModel : IReadModel
        {
            throw new NotImplementedException();
        }

        public Task PopulateAggregateReadModelAsync<TAggregate, TIdentity, TReadModel>(TIdentity id,
            CancellationToken cancellationToken) where TAggregate : IAggregateRoot<TIdentity> where TIdentity : IIdentity where TReadModel : IReadModel
        {
            throw new NotImplementedException();
        }
    }

    /*
    public class ReadModelPopulator : IReadModelPopulator
    {
        private readonly ILog _log;
        private readonly IEventFlowConfiguration _configuration;
        private readonly IEventStore _eventStore;
        private readonly IReadOnlyCollection<IReadModelStore> _readModelStores;

        public ReadModelPopulator(
            ILog log,
            IEventFlowConfiguration configuration,
            IEventStore eventStore,
            IEnumerable<IReadModelStore> readModelStores)
        {
            _log = log;
            _configuration = configuration;
            _eventStore = eventStore;
            _readModelStores = readModelStores.ToList();
        }

        public Task PurgeAsync<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            var purgeTasks = _readModelStores.Select(s => s.PurgeAsync<TReadModel>(cancellationToken));
            return Task.WhenAll(purgeTasks);
        }

        public void Purge<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PurgeAsync<TReadModel>(cancellationToken));
            }
        }

        public async Task PopulateAsync<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            var stopwatch = Stopwatch.StartNew();

            var readModelType = typeof (TReadModel);
            var aggregateEventTypes = new HashSet<Type>(readModelType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IAmReadModelFor<,,>))
                .Select(i => i.GetGenericArguments()[2]));

            _log.Verbose(() => string.Format(
                "Read model '{0}' is interested in these aggregate events: {1}",
                readModelType.Name,
                string.Join(", ", aggregateEventTypes.Select(e => e.Name).OrderBy(s => s))));

            long totalEvents = 0;
            long relevantEvents = 0;
            long currentPosition = 0;

            while (true)
            {
                _log.Verbose(
                    "Loading events starting from {0} and the next {1} for populating '{2}'",
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    readModelType.Name);
                var allEventsPage = await _eventStore.LoadAllEventsAsync(
                    currentPosition,
                    _configuration.PopulateReadModelEventPageSize,
                    cancellationToken)
                    .ConfigureAwait(false);
                totalEvents += allEventsPage.DomainEvents.Count;
                currentPosition = allEventsPage.NextPosition;

                if (!allEventsPage.DomainEvents.Any())
                {
                    _log.Verbose("No more events in event store, stopping population of read model '{0}'", readModelType.Name);
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

                var applyTasks = _readModelStores
                    .Select(rms => rms.ApplyDomainEventsAsync<TReadModel>(domainEvents, cancellationToken));
                await Task.WhenAll(applyTasks).ConfigureAwait(false);
            }

            stopwatch.Stop();
            _log.Information(
                "Population of read model '{0}' took {1:0.###} seconds, in which {2} events was loaded and {3} was relevant",
                readModelType.Name,
                stopwatch.Elapsed.TotalSeconds,
                totalEvents,
                relevantEvents);
        }

        public async Task PopulateAggregateReadModelAsync<TAggregate, TIdentity, TReadModel>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TReadModel : IReadModel
        {
            var readModelType = typeof (TReadModel);
            var iAmReadModelForInterfaceTypes = readModelType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IAmReadModelFor<,,>))
                .ToList();
            var aggregateTypes = iAmReadModelForInterfaceTypes
                .Select(i => i.GetGenericArguments()[0])
                .Distinct()
                .ToList();

            if (!aggregateTypes.Any())
            {
                throw new ArgumentException(string.Format(
                    "Read model type '{0}' does not implement 'IAmReadModelFor<>'",
                    readModelType.Name));
            }
            if (aggregateTypes.Count != 1)
            {
                throw new ArgumentException(string.Format(
                    "Use 'PopulateAsync' to populate read models than registers events from more than one aggregate. '{0}' registers to these: {1}",
                    readModelType.Name,
                    string.Join(", ", aggregateTypes.Select(t => t.Name))));
            }
            if (aggregateTypes.Single() != typeof (TReadModel))
            {
                throw new ArgumentException(string.Format(
                    "Read model '{0}' registers to aggregate '{1}', but you supplied '{2}' as the generic argument",
                    readModelType.Name,
                    aggregateTypes.Single().Name,
                    typeof(TAggregate).Name));
            }

            var domainEvens = await _eventStore.LoadEventsAsync<TAggregate, TIdentity>(
                id,
                cancellationToken)
                .ConfigureAwait(false);

            var readModelContext = new ReadModelContext();

            var populateTasks = _readModelStores
                .Select(s => s.PopulateReadModelAsync<TReadModel>(
                    id.Value,
                    domainEvens,
                    readModelContext,
                    cancellationToken));

            await Task.WhenAll(populateTasks).ConfigureAwait(false);
        }

        public void Populate<TReadModel>(CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(PopulateAsync<TReadModel>(cancellationToken));
            }
        }
    }
    */
}
