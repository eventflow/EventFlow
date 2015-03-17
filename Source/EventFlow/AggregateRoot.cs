// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventFlow.EventStores;

namespace EventFlow
{
    public abstract class AggregateRoot<TAggregate> : IAggregateRoot
        where TAggregate : AggregateRoot<TAggregate>
    {
        public string Id { get; private set; }
        public int Version { get; private set; }
        public bool IsNew { get { return Version <= 0; } }

        private readonly List<IUncommittedDomainEvent> _uncommittedDomainEvents = new List<IUncommittedDomainEvent>(); 

        protected AggregateRoot(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentNullException("id");
            }
            if (id.Length > 255)
            {
                throw new ArgumentException(
                    string.Format("Aggregate IDs must not exceed 255 in length and '{0}' is too long with a length of {1}", id, id.Length),
                    "id");
            }

            Id = id;
        }

        protected void Emit<TEvent>(TEvent aggregateEvent, IMetadata metadata = null)
            where TEvent : AggregateEvent<TAggregate>
        {
            if (aggregateEvent == null)
            {
                throw new ArgumentNullException("aggregateEvent");
            }

            var extraMetadata = new Dictionary<string, string>
                {
                    {MetadataKeys.Timestamp, DateTimeOffset.Now.ToString("o")}
                };

            metadata = metadata == null
                ? new Metadata(extraMetadata)
                : metadata.CloneWith(extraMetadata);

            var uncommittedDomainEvent = new UncommittedDomainEvent(aggregateEvent, metadata);

            ApplyEvent(aggregateEvent);
            _uncommittedDomainEvents.Add(uncommittedDomainEvent);
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> CommitAsync(IEventStore eventStore)
        {
            var oldVersion = Version - _uncommittedDomainEvents.Count;
            var domainEvents = await eventStore.StoreAsync<TAggregate>(Id, oldVersion, Version, _uncommittedDomainEvents).ConfigureAwait(false);
            _uncommittedDomainEvents.Clear();
            return domainEvents;
        }

        public void ApplyEvents(IEnumerable<IAggregateEvent> domainEvents)
        {
            if (Version > 0)
            {
                throw new InvalidOperationException("Aggregate already has events");
            }

            foreach (var domainEvent in domainEvents)
            {
                ApplyEvent(domainEvent);
            }
        }

        private void ApplyEvent(IAggregateEvent aggregateEvent)
        {
            var applyMethod = GetApplyMethod(aggregateEvent.GetType());
            applyMethod(this as TAggregate, aggregateEvent);
            Version++;
        }

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>> ApplyMethods = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>>(); 
        private Action<TAggregate, IAggregateEvent> GetApplyMethod(Type domainEventType)
        {
            var aggregateType = GetType();
            var typeDictionary = ApplyMethods.GetOrAdd(
                aggregateType,
                t => new ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>());

            var applyMethod = typeDictionary.GetOrAdd(
                domainEventType,
                t =>
                    {
                        var m = aggregateType.GetMethod("Apply", new []{ t });
                        return (a, e) => m.Invoke(a, new object[] {e});
                    });

            return applyMethod;
        }
    }
}
