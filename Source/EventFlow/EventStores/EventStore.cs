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
using EventFlow.EventCaches.InMemory;
using EventFlow.Exceptions;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public abstract class EventStore : IEventStore
    {
        protected ILog Log { get; private set; }
        protected IAggregateFactory AggregateFactory { get; private set; }
        protected IEventJsonSerializer EventJsonSerializer { get; private set; }
        protected IEventCache EventCache { get; private set; }
        protected IReadOnlyCollection<IMetadataProvider> MetadataProviders { get; private set; }

        protected EventStore(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEventCache eventCache,
            IEnumerable<IMetadataProvider> metadataProviders)
        {
            Log = log;
            AggregateFactory = aggregateFactory;
            EventJsonSerializer = eventJsonSerializer;
            EventCache = eventCache;
            MetadataProviders = metadataProviders.ToList();
        }

        public virtual async Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate>(
            string id,
            IReadOnlyCollection<IUncommittedEvent> uncommittedDomainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
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
                            .SelectMany(p => p.ProvideMetadata<TAggregate>(id, e.AggregateEvent, e.Metadata))
                            .Concat(e.Metadata);
                        return EventJsonSerializer.Serialize(e.AggregateEvent, metadata);
                    })
                .ToList();

            IReadOnlyCollection<ICommittedDomainEvent> committedDomainEvents;
            try
            {
                committedDomainEvents = await CommitEventsAsync<TAggregate>(
                    id,
                    serializedEvents,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (OptimisticConcurrencyException)
            {
                Log.Verbose(
                    "Detected a optimisting concurrency exception for aggregate '{0}' with ID '{1}', invalidating cache",
                    aggregateType.Name,
                    id);

                // TODO: Rework as soon as await is possible within catch
                using (var a = AsyncHelper.Wait)
                {
                    a.Run(EventCache.InvalidateAsync(aggregateType, id, cancellationToken));
                }

                throw;
            }

            var domainEvents = committedDomainEvents.Select(EventJsonSerializer.Deserialize).ToList();

            await EventCache.InsertAsync(aggregateType, id, domainEvents, cancellationToken).ConfigureAwait(false);

            return domainEvents;
        }

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate>(
            string id, IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot;

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken);

        public virtual async Task<IReadOnlyCollection<IDomainEvent>> LoadEventsAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof (TAggregate);
            var cachedDomainEvents = await EventCache.GetAsync(aggregateType, id, cancellationToken).ConfigureAwait(false);
            if (cachedDomainEvents != null)
            {
                return cachedDomainEvents;
            }

            var committedDomainEvents = await LoadCommittedEventsAsync<TAggregate>(id, cancellationToken).ConfigureAwait(false);
            var domainEvents = committedDomainEvents
                .Select(EventJsonSerializer.Deserialize)
                .ToList();
            return domainEvents;
        }

        public virtual async Task<TAggregate> LoadAggregateAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof(TAggregate);

            Log.Verbose(
                "Loading aggregate '{0}' with ID '{1}'",
                aggregateType.Name,
                id);
            
            var domainEvents = await LoadEventsAsync<TAggregate>(id, cancellationToken).ConfigureAwait(false);
            var aggregate = await AggregateFactory.CreateNewAggregateAsync<TAggregate>(id).ConfigureAwait(false);
            aggregate.ApplyEvents(domainEvents.Select(e => e.GetAggregateEvent()));

            Log.Verbose(
                "Done loading aggregate '{0}' with ID '{1}' after applying {2} events",
                aggregateType.Name,
                id,
                domainEvents.Count);

            return aggregate;
        }

        public virtual TAggregate LoadAggregate<TAggregate>(string id, CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot
        {
            var aggregate = default(TAggregate);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadAggregateAsync<TAggregate>(id, cancellationToken), r => aggregate = r);
            }
            return aggregate;
        }
    }
}
