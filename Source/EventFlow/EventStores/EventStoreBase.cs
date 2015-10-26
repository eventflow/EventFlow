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
using EventFlow.Entities;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public class EventStoreBase : IEventStore
    {
        private readonly IAggregateFactory _aggregateFactory;
        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly IEventPersistence _eventPersistence;
        private readonly IEventUpgradeManager _eventUpgradeManager;
        private readonly ILog _log;
        private readonly IReadOnlyCollection<IMetadataProvider> _metadataProviders;

        public EventStoreBase(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventUpgradeManager eventUpgradeManager,
            IEnumerable<IMetadataProvider> metadataProviders,
            IEventPersistence eventPersistence)
        {
            _eventPersistence = eventPersistence;
            _log = log;
            _aggregateFactory = aggregateFactory;
            _eventJsonSerializer = eventJsonSerializer;
            _eventUpgradeManager = eventUpgradeManager;
            _metadataProviders = metadataProviders.ToList();
        }

        public virtual async Task<IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>> StoreAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<IUncommittedEvent> uncommittedDomainEvents,
            ISourceId sourceId,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            if (id == null) throw new ArgumentNullException(nameof(id));
            if (sourceId.IsNone()) throw new ArgumentNullException(nameof(sourceId));

            if (uncommittedDomainEvents == null || !uncommittedDomainEvents.Any())
            {
                return new IDomainEvent<TAggregate, TIdentity>[] {};
            }

            var aggregateType = typeof (TAggregate);
            _log.Verbose(() => $"Storing {uncommittedDomainEvents.Count} events for aggregate '{aggregateType.PrettyPrint()}' with ID '{id}'");

            var batchId = Guid.NewGuid().ToString();
            var storeMetadata = new[]
                {
                    new KeyValuePair<string, string>(MetadataKeys.BatchId, batchId),
                    new KeyValuePair<string, string>(MetadataKeys.SourceId, sourceId.Value)
                };

            var serializedEvents = uncommittedDomainEvents
                .Select(e =>
                    {
                        var md = _metadataProviders
                            .SelectMany(p => p.ProvideMetadata<TAggregate, TIdentity>(id, e.AggregateEvent, e.Metadata))
                            .Concat(e.Metadata)
                            .Concat(storeMetadata);
                        return _eventJsonSerializer.Serialize(e.AggregateEvent, md);
                    })
                .ToList();

            var committedDomainEvents = await _eventPersistence.CommitEventsAsync(
                id,
                serializedEvents,
                cancellationToken)
                .ConfigureAwait(false);

            var domainEvents = committedDomainEvents
                .Select(e => _eventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            return domainEvents;
        }

        public async Task<AllEventsPage> LoadAllEventsAsync(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            var allCommittedEventsPage = await _eventPersistence.LoadAllCommittedEvents(
                globalPosition,
                pageSize,
                cancellationToken)
                .ConfigureAwait(false);
            var domainEvents = (IReadOnlyCollection<IDomainEvent>) allCommittedEventsPage.CommittedDomainEvents
                .Select(e => _eventJsonSerializer.Deserialize(e))
                .ToList();
            domainEvents = _eventUpgradeManager.Upgrade(domainEvents);
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

        public virtual async Task<IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>> LoadEventsAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var committedDomainEvents = await _eventPersistence.LoadCommittedEventsAsync(
                id,
                cancellationToken)
                .ConfigureAwait(false);
            var domainEvents = (IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>>) committedDomainEvents
                .Select(e => _eventJsonSerializer.Deserialize<TAggregate, TIdentity>(id, e))
                .ToList();

            if (!domainEvents.Any())
            {
                return domainEvents;
            }

            domainEvents = _eventUpgradeManager.Upgrade(domainEvents);

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
            var aggregateType = typeof (TAggregate);

            _log.Verbose(
                "Loading aggregate '{0}' with ID '{1}'",
                aggregateType.Name,
                id);

            var domainEvents = await LoadEventsAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
            var aggregate = await _aggregateFactory.CreateNewAggregateAsync<TAggregate, TIdentity>(id).ConfigureAwait(false);
            aggregate.ApplyEvents(domainEvents);

            _log.Verbose(() => $"Done loading aggregate '{aggregateType.PrettyPrint()}' with ID '{id}' after applying {domainEvents.Count} events");

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

        public Task DeleteAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return _eventPersistence.DeleteEventsAsync(
                id,
                cancellationToken);
        }
    }
}