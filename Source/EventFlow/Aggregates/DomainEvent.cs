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
using EventFlow.Core;
using EventFlow.EventSource;

namespace EventFlow.Aggregates
{
    public class DomainEvent<TEventSourcedEntity, TIdentity, TSourceEvent> : IDomainEvent<TEventSourcedEntity, TIdentity, TSourceEvent>
        where TEventSourcedEntity : IEventSourcedEntity<TIdentity>
        where TIdentity : IIdentity
        where TSourceEvent : ISourceEvent<TEventSourcedEntity, TIdentity>
    {
        public TIdentity AggregateIdentity => EntityId;
        public Type AggregateType => EntityType;
        public int AggregateSequenceNumber => SequenceNumber;
        public TSourceEvent AggregateEvent => SourceEvent;

        public Type EntityType => typeof (TEventSourcedEntity);
        public Type EventType => typeof (TSourceEvent);
        public TSourceEvent SourceEvent { get; }
        public TIdentity EntityId { get; }
        public IMetadata Metadata { get; }
        public DateTimeOffset Timestamp { get; }
        public int SequenceNumber { get; }

        public DomainEvent(
            TSourceEvent sourceEvent,
            IMetadata metadata,
            DateTimeOffset timestamp,
            TIdentity entityId,
            int sequenceNumber)
        {
            if (sourceEvent == null) throw new ArgumentNullException(nameof(sourceEvent));
            if (metadata == null) throw new ArgumentNullException(nameof(metadata));
            if (timestamp == default(DateTimeOffset)) throw new ArgumentNullException(nameof(timestamp));
            if (entityId == null || string.IsNullOrEmpty(entityId.Value)) throw new ArgumentNullException(nameof(entityId));
            if (sequenceNumber <= 0) throw new ArgumentOutOfRangeException(nameof(sequenceNumber));

            SourceEvent = sourceEvent;
            Metadata = metadata;
            Timestamp = timestamp;
            EntityId = entityId;
            SequenceNumber = sequenceNumber;
        }

        public IIdentity GetIdentity()
        {
            return AggregateIdentity;
        }

        public IAggregateEvent GetAggregateEvent()
        {
            return (IAggregateEvent) GetSourceEvent();
        }

        public ISourceEvent GetSourceEvent()
        {
            throw new NotImplementedException();
        }

        public override string ToString()
        {
            return $"{AggregateType.Name} v{AggregateSequenceNumber}/{EventType.Name}:{AggregateIdentity}";
        }
    }
}