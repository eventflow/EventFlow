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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;

namespace EventFlow.Aggregates
{
    public abstract class AggregateRoot<TAggregate, TIdentity> : IAggregateRoot<TIdentity>
        where TAggregate : AggregateRoot<TAggregate, TIdentity>
        where TIdentity : IIdentity
    {
        private readonly List<IUncommittedEvent> _uncommittedEvents = new List<IUncommittedEvent>();

        public TIdentity Id { get; private set; }
        public int Version { get; private set; }
        public bool IsNew { get { return Version <= 0; } }
        public IEnumerable<IAggregateEvent> UncommittedEvents { get { return _uncommittedEvents.Select(e => e.AggregateEvent); } }

        protected AggregateRoot(TIdentity id)
        {
            if (id == null) throw new ArgumentNullException("id");
            if ((this as TAggregate) == null)
            {
                throw WrongImplementationException.With(
                    HelpLinkType.Aggregates,
                    "Aggregate '{0}' specifies '{1}' as generic argument, it should be its own type",
                    GetType().Name,
                    typeof(TAggregate).Name);
            }

            Id = id;
        }

        protected virtual void Emit<TEvent>(TEvent aggregateEvent, IMetadata metadata = null)
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
        {
            if (aggregateEvent == null)
            {
                throw new ArgumentNullException("aggregateEvent");
            }

            var now = DateTimeOffset.Now;
            var extraMetadata = new Dictionary<string, string>
                {
                    {MetadataKeys.Timestamp, now.ToString("o")},
                    {MetadataKeys.TimestampEpoch, now.ToUnixTime().ToString()},
                    {MetadataKeys.AggregateSequenceNumber, (Version + 1).ToString()},
                    {MetadataKeys.AggregateName, GetType().Name.Replace("Aggregate", string.Empty)},
                };

            metadata = metadata == null
                ? new Metadata(extraMetadata)
                : metadata.CloneWith(extraMetadata);

            var uncommittedEvent = new UncommittedEvent(aggregateEvent, metadata);

            ApplyEvent(aggregateEvent);
            _uncommittedEvents.Add(uncommittedEvent);
        }

        public async Task<IReadOnlyCollection<IDomainEvent>> CommitAsync(
            IEventStore eventStore,
            CancellationToken cancellationToken)
        {
            var domainEvents = await eventStore.StoreAsync<TAggregate, TIdentity>(
                Id,
                _uncommittedEvents,
                cancellationToken)
                .ConfigureAwait(false);
            _uncommittedEvents.Clear();
            return domainEvents;
        }

        public void ApplyEvents(IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            if (!domainEvents.Any())
            {
                return;
            }

            ApplyEvents(domainEvents.Select(e => e.GetAggregateEvent()));
            Version = domainEvents.Max(e => e.AggregateSequenceNumber);
        }

        public void ApplyEvents(IEnumerable<IAggregateEvent> aggregateEvents)
        {
            if (Version > 0)
            {
                throw new InvalidOperationException(string.Format(
                    "Aggregate '{0}' with ID '{1}' already has events",
                    GetType().Name,
                    Id));
            }

            foreach (var aggregateEvent in aggregateEvents)
            {
                var e = aggregateEvent as IAggregateEvent<TAggregate, TIdentity>;
                if (e == null)
                {
                    throw new ArgumentException(string.Format(
                        "Aggregate event of type '{0}' does not belong with aggregate '{1}',",
                        aggregateEvent.GetType(),
                        this));
                }

                ApplyEvent(e);
            }
        }

        protected virtual void ApplyEvent(IAggregateEvent<TAggregate, TIdentity> aggregateEvent)
        {
            var eventType = aggregateEvent.GetType();
            if (_eventHandlers.ContainsKey(eventType))
            {
                _eventHandlers[eventType](aggregateEvent);
            }
            else if (_eventApplier != null)
            {
                _eventApplier.Apply((TAggregate)this, aggregateEvent);
            }
            else
            {
                var applyMethod = GetApplyMethod(eventType);
                applyMethod(this as TAggregate, aggregateEvent);
            }
            Version++;
        }

        private readonly Dictionary<Type, Action<object>> _eventHandlers = new Dictionary<Type, Action<object>>();
        protected void Register<TAggregateEvent>(Action<TAggregateEvent> handler)
            where TAggregateEvent : IAggregateEvent<TAggregate, TIdentity>
        {
            var eventType = typeof (TAggregateEvent);
            if (_eventHandlers.ContainsKey(eventType))
            {
                throw new ArgumentException(string.Format(
                    "There's already a event handler registered for the aggregate event '{0}'",
                    eventType.Name));
            }
            _eventHandlers[eventType] = e => handler((TAggregateEvent)e);
        }

        private IEventApplier<TAggregate, TIdentity> _eventApplier;

        protected void Register(IEventApplier<TAggregate, TIdentity> eventApplier)
        {
            if (_eventApplier != null)
            {
                throw new InvalidOperationException("You cannot apply an event applier as its already configured");
            }
            _eventApplier = eventApplier;
        }

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>> ApplyMethods = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>>(); 
        protected Action<TAggregate, IAggregateEvent> GetApplyMethod(Type aggregateEventType)
        {
            var aggregateType = GetType();
            var typeDictionary = ApplyMethods.GetOrAdd(
                aggregateType,
                t => new ConcurrentDictionary<Type, Action<TAggregate, IAggregateEvent>>());

            var applyMethod = typeDictionary.GetOrAdd(
                aggregateEventType,
                t =>
                    {
                        var m = aggregateType.GetMethod(
                            "Apply",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null,
                            new []{ t },
                            null);
                        if (m == null)
                        {
                            throw WrongImplementationException.With(
                                HelpLinkType.Aggregates,
                                "Aggregate type '{0}' doesn't implement an 'Apply' method for the event '{1}'. Implement IEmit<{1}>",
                                GetType().Name,
                                t.Name);
                        }
                        return (a, e) => m.Invoke(a, new object[] {e});
                    });

            return applyMethod;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} v{1}(-{2})",
                GetType().Name,
                Version,
                _uncommittedEvents.Count);
        }
    }
}
