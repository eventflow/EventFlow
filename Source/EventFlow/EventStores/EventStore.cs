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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventCaches;
using EventFlow.Exceptions;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public abstract class EventStore : IEventStore
    {
        protected ILog Log { get; private set; }
        protected IAggregateFactory AggregateFactory { get; private set; }
        protected IEventUpgradeManager EventUpgradeManager { get; private set; }
        protected IEventJsonSerializer EventJsonSerializer { get; private set; }
        protected IEventCache EventCache { get; private set; }
        protected IReadOnlyCollection<IMetadataProvider> MetadataProviders { get; private set; }

        protected EventStore(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventCache eventCache,
            IEventUpgradeManager eventUpgradeManager,
            IEnumerable<IMetadataProvider> metadataProviders)
        {
            Log = log;
            AggregateFactory = aggregateFactory;
            EventJsonSerializer = eventJsonSerializer;
            EventCache = eventCache;
            EventUpgradeManager = eventUpgradeManager;
            MetadataProviders = metadataProviders.ToList();
        }

        public virtual async Task<IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>> StoreAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<IUncommittedEvent> uncommittedDomainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            if (!uncommittedDomainEvents.Any())
            {
                return new IDomainEvent<TAggregate, TIdentity>[] {};
            }

            var aggregateType = typeof (TAggregate);
            Log.Verbose(
                "Storing {0} events for aggregate '{1}' with ID '{2}'",
                uncommittedDomainEvents.Count,
                aggregateType.Name,
                id);

            var serializedEvents = uncommittedDomainEvents
                .Select(e =>
                    {
                        var metadata = MetadataProviders
                            .SelectMany(p => p.ProvideMetadata<TAggregate, TIdentity>(id, e.AggregateEvent, e.Metadata))
                            .Concat(e.Metadata);
                        return EventJsonSerializer.Serialize(e.AggregateEvent, metadata);
                    })
                .ToList();

            IReadOnlyCollection<ICommittedDomainEvent> committedDomainEvents;
            try
            {
                committedDomainEvents = await CommitEventsAsync<TAggregate, TIdentity>(
                    id,
                    serializedEvents,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OptimisticConcurrencyException)
            {
                Log.Verbose(
                    "Detected an optimisting concurrency exception for aggregate '{0}' with ID '{1}', invalidating cache",
                    aggregateType.Name,
                    id);

                // TODO: Rework as soon as await is possible within catch
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(EventCache.InvalidateAsync<TAggregate, TIdentity>(id, cancellationToken));
                }

                throw;
            }

            var domainEvents = committedDomainEvents
                .Select(e => EventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            await EventCache.InvalidateAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);

            return domainEvents;
        }

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity;

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity;

        public virtual async Task<IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>> LoadEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var domainEvents = await EventCache.GetAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
            if (domainEvents != null)
            {
                return domainEvents;
            }

            var committedDomainEvents = await LoadCommittedEventsAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
            domainEvents = committedDomainEvents
                .Select(e => EventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            if (!domainEvents.Any())
            {
                return domainEvents;
            }

            domainEvents = EventUpgradeManager.Upgrade(domainEvents);

            await EventCache.InsertAsync(id, domainEvents, cancellationToken).ConfigureAwait(false);

            return domainEvents;
        }

        public IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> LoadEvents<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity> where TIdentity : IIdentity
        {
            IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> domainEvents = null;
            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadEventsAsync<TAggregate, TIdentity>(id, cancellationToken), d => domainEvents = d);
            }
            return domainEvents;
        }

        public virtual async Task<TAggregate> LoadAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateType = typeof(TAggregate);

            Log.Verbose(
                "Loading aggregate '{0}' with ID '{1}'",
                aggregateType.Name,
                id);
            
            var domainEvents = await LoadEventsAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
            var aggregate = await AggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(id).ConfigureAwait(false);
            aggregate.ApplyEvents(domainEvents);

            Log.Verbose(
                "Done loading aggregate '{0}' with ID '{1}' after applying {2} events",
                aggregateType.Name,
                id,
                domainEvents.Count);

            return aggregate;
        }

        public virtual TAggregate LoadAggregate<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregate = default(TAggregate);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadAggregateAsync<TAggregate, TIdentity>(id, cancellationToken), r => aggregate = r);
            }
            return aggregate;
        }

        public abstract Task DeleteAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity;
    }
}
