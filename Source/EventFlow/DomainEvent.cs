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

namespace EventFlow
{
    public class DomainEvent<TAggregateEvent> : IDomainEvent<TAggregateEvent>
        where TAggregateEvent : IAggregateEvent
    {
        public Type AggregateType { get { return AggregateEvent.GetAggregateType(); } }
        public Type EventType { get { return typeof (TAggregateEvent); } }

        public int AggregateSequenceNumber { get; private set; }
        public Guid BatchId { get; private set; }
        public TAggregateEvent AggregateEvent { get; private set; }
        public long GlobalSequenceNumber { get; private set; }
        public string AggregateId { get; private set; }
        public IMetadata Metadata { get; private set; }
        public DateTimeOffset Timestamp { get; private set; }

        public DomainEvent(
            TAggregateEvent aggregateEvent,
            IMetadata metadata,
            DateTimeOffset timestamp,
            long globalSequenceNumber,
            string aggregateId,
            int aggregateSequenceNumber,
            Guid batchId)
        {
            AggregateEvent = aggregateEvent;
            Metadata = metadata;
            Timestamp = timestamp;
            GlobalSequenceNumber = globalSequenceNumber;
            AggregateId = aggregateId;
            AggregateSequenceNumber = aggregateSequenceNumber;
            BatchId = batchId;
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
                AggregateId);
        }
    }
}
