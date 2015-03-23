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
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public abstract class EventStore : IEventStore
    {
        protected ILog Log { get; private set; }
        protected IEventJsonSerializer EventJsonSerializer { get; private set; }
        protected IReadOnlyCollection<IMetadataProvider> MetadataProviders { get; private set; }

        protected EventStore(
            ILog log,
            IEventJsonSerializer eventJsonSerializer,
            IEnumerable<IMetadataProvider> metadataProviders)
        {
            Log = log;
            EventJsonSerializer = eventJsonSerializer;
            MetadataProviders = metadataProviders.ToList();
        }

        public virtual async Task<IReadOnlyCollection<IDomainEvent>> StoreAsync<TAggregate>(string id, IReadOnlyCollection<IUncommittedDomainEvent> uncommittedDomainEvents)
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

            var committedDomainEvents = await CommitEventsAsync<TAggregate>(id, serializedEvents).ConfigureAwait(false);

            var domainEvents = committedDomainEvents.Select(EventJsonSerializer.Deserialize).ToList();
            
            return domainEvents;
        }

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate>(string id, IReadOnlyCollection<SerializedEvent> serializedEvents)
            where TAggregate : IAggregateRoot;

        protected abstract Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(string id);

        public virtual async Task<IReadOnlyCollection<IDomainEvent>> LoadEventsAsync(string id)
        {
            var committedDomainEvents = await LoadCommittedEventsAsync(id).ConfigureAwait(false);
            var domainEvents = committedDomainEvents
                .Select(EventJsonSerializer.Deserialize)
                .ToList();
            return domainEvents;
        }

        public virtual async Task<TAggregate> LoadAggregateAsync<TAggregate>(string id)
            where TAggregate : IAggregateRoot
        {
            var aggregateType = typeof(TAggregate);

            Log.Verbose(
                "Loading aggregate '{0}' with ID '{1}'",
                aggregateType.Name,
                id);
            
            var domainEvents = await LoadEventsAsync(id).ConfigureAwait(false);
            var aggregate = (TAggregate)Activator.CreateInstance(aggregateType, id);
            aggregate.ApplyEvents(domainEvents.Select(e => e.GetAggregateEvent()));

            Log.Verbose(
                "Done loading aggregate '{0}' with ID '{1}' after applying {2} events",
                aggregateType.Name,
                id,
                domainEvents.Count);

            return aggregate;
        }

        public virtual TAggregate LoadAggregate<TAggregate>(string id) where TAggregate : IAggregateRoot
        {
            var aggregate = default(TAggregate);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(LoadAggregateAsync<TAggregate>(id), r => aggregate = r);
            }
            return aggregate;
        }
    }
}
