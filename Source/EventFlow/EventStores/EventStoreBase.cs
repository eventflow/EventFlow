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
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public abstract class EventStoreBase : IEventStore
    {
        protected class AllCommittedEventsPage
        {
            public GlobalPosition NextGlobalPosition { get; }
            public IReadOnlyCollection<ICommittedDomainEvent> CommittedDomainEvents { get; }

            public AllCommittedEventsPage(
                GlobalPosition nextGlobalPosition,
                IReadOnlyCollection<ICommittedDomainEvent> committedDomainEvents)
            {
                NextGlobalPosition = nextGlobalPosition;
                CommittedDomainEvents = committedDomainEvents;
            }
        }

        protected ILog Log { get; }
        protected IAggregateFactory AggregateFactory { get; }
        protected IEventUpgradeManager EventUpgradeManager { get; }
        protected IEventJsonSerializer EventJsonSerializer { get; }
        protected IReadOnlyCollection<IMetadataProvider> MetadataProviders { get; }

        protected EventStoreBase(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventUpgradeManager eventUpgradeManager,
            IEnumerable<IMetadataProvider> metadataProviders)
        {
            Log = log;
            AggregateFactory = aggregateFactory;
            EventJsonSerializer = eventJsonSerializer;
            EventUpgradeManager = eventUpgradeManager;
            MetadataProviders = metadataProviders.ToList();
        }

        public virtual async Task<IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>> StoreAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<IUncommittedEvent> uncommittedDomainEvents,
            ISourceId sourceId,
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

            var batchId = Guid.NewGuid().ToString();
            var storeMetadata = new []
                {
                    new KeyValuePair<string, string>(MetadataKeys.BatchId, batchId),
                    new KeyValuePair<string, string>(MetadataKeys.SourceId, sourceId.Value),
                };

            var serializedEvents = uncommittedDomainEvents
                .Select(e =>
                    {
                        var md = MetadataProviders
                            .SelectMany(p => p.ProvideMetadata<TAggregate, TIdentity>(id, e.AggregateEvent, e.Metadata))
                            .Concat(e.Metadata)
                            .Concat(storeMetadata);
                        return EventJsonSerializer.Serialize(e.AggregateEvent, md);
                    })
                .ToList();

            var committedDomainEvents = await CommitEventsAsync<TAggregate, TIdentity>(
                id,
                serializedEvents,
                cancellationToken)
                .ConfigureAwait(false);

            var domainEvents = committedDomainEvents
                .Select(e => EventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            return domainEvents;
        }

        public async Task<AllEventsPage> LoadAllEventsAsync(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var allCommittedEventsPage = await LoadAllCommittedDomainEvents(
                globalPosition,
                pageSize,
                cancellationToken)
                .ConfigureAwait(false);
            var domainEvents = (IReadOnlyCollection<IDomainEvent>)allCommittedEventsPage.CommittedDomainEvents
                .Select(e => EventJsonSerializer.Deserialize(e))
                .ToList();
            domainEvents = EventUpgradeManager.Upgrade(domainEvents);
            return new AllEventsPage(allCommittedEventsPage.NextGlobalPosition, domainEvents);
        }

        public AllEventsPage LoadAllEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
        {
            AllEventsPage allEventsPage = null;
            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadAllEventsAsync(globalPosition, pageSize, cancellationToken), p => allEventsPage = p);
            }
            return allEventsPage;
        }

        protected abstract Task<AllCommittedEventsPage> LoadAllCommittedDomainEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken);

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
            var committedDomainEvents = await LoadCommittedEventsAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
            var domainEvents = (IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>)committedDomainEvents
                .Select(e => EventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            if (!domainEvents.Any())
            {
                return domainEvents;
            }

            domainEvents = EventUpgradeManager.Upgrade(domainEvents);

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
