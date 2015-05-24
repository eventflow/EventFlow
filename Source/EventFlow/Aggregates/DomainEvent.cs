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

namespace EventFlow.Aggregates
{
    public class DomainEvent<TAggregate, TIdentity, TAggregateEvent> : IDomainEvent<TAggregate, TIdentity, TAggregateEvent>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TAggregateEvent : IAggregateEvent<TAggregate, TIdentity>
    {
        public Type AggregateType { get { return typeof (TAggregate); } }
        public Type EventType { get { return typeof (TAggregateEvent); } }

        public int AggregateSequenceNumber { get; private set; }
        public Guid BatchId { get; private set; }
        public TAggregateEvent AggregateEvent { get; private set; }
        public TIdentity AggregateIdentity { get; private set; }
        public IMetadata Metadata { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public DomainEvent(
            TAggregateEvent aggregateEvent,
            IMetadata metadata,
            DateTimeOffset timestamp,
            TIdentity aggregateIdentity,
            int aggregateSequenceNumber,
            Guid batchId)
        {
            AggregateEvent = aggregateEvent;
            Metadata = metadata;
            Timestamp = timestamp;
            AggregateIdentity = aggregateIdentity;
            AggregateSequenceNumber = aggregateSequenceNumber;
            BatchId = batchId;
        }

        public IIdentity GetIdentity()
        {
            return AggregateIdentity;
        }

        public IAggregateEvent GetAggregateEvent()
        {
            return AggregateEvent;
        }

        public override string ToString()
        {
            return string.Format(
                "{0} v{1}/{2}:{3}",
                AggregateType.Name,
                AggregateSequenceNumber,
                EventType.Name,
                AggregateIdentity);
        }
    }
}
