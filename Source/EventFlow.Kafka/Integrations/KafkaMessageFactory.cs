// The MIT License (MIT)
//
// Copyright (c) 2020 Rasmus Mikkelsen
// Copyright (c) 2020 eBay Software Foundation
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

using EventFlow.Aggregates;
using EventFlow.Extensions;
using System;


namespace EventFlow.Kafka.Integrations
{
    class KafkaMessageFactory : IKafkaMessageFactory
    {
        public KafkaMessage CreateMessage(IDomainEvent domainEvent)
        {
            return new KafkaMessage
            {
                AggregateId = domainEvent.Metadata.AggregateId,
                AggregateName = domainEvent.Metadata[MetadataKeys.AggregateName],
                BatchId = Guid.Parse(domainEvent.Metadata[MetadataKeys.BatchId]),
                Data = domainEvent.GetAggregateEvent(),
                Metadata = domainEvent.Metadata,
                AggregateSequenceNumber = domainEvent.AggregateSequenceNumber,
                MessageId = new MessageId(domainEvent.Metadata[MetadataKeys.EventId]),
                Topic = domainEvent.Metadata[MetadataKeys.AggregateName].ToSlug()
            };
        }
    }
}
