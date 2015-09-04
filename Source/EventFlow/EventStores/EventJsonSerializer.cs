﻿// The MIT License (MIT)
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
using System.Globalization;
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventSourcing;

namespace EventFlow.EventStores
{
    public class EventJsonSerializer : IEventJsonSerializer
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IEventDefinitionService _eventDefinitionService;
        private readonly IDomainEventFactory _domainEventFactory;

        public EventJsonSerializer(
            IJsonSerializer jsonSerializer,
            IEventDefinitionService eventDefinitionService,
            IDomainEventFactory domainEventFactory)
        {
            _jsonSerializer = jsonSerializer;
            _eventDefinitionService = eventDefinitionService;
            _domainEventFactory = domainEventFactory;
        }

        public SerializedEvent Serialize(IEvent @event, IEnumerable<KeyValuePair<string, string>> metadatas)
        {
            var eventDefinition = _eventDefinitionService.GetEventDefinition(@event.GetType());

            var metadata = new Metadata(metadatas
                .Where(kv => kv.Key != MetadataKeys.EventName && kv.Key != MetadataKeys.EventVersion) // TODO: Fix this
                .Concat(new[]
                    {
                        new KeyValuePair<string, string>(MetadataKeys.EventName, eventDefinition.Name),
                        new KeyValuePair<string, string>(MetadataKeys.EventVersion, eventDefinition.Version.ToString(CultureInfo.InvariantCulture)),
                    }));

            var dataJson = _jsonSerializer.Serialize(@event);
            var metaJson = _jsonSerializer.Serialize(metadata);

            return new SerializedEvent(
                metaJson,
                dataJson,
                metadata.AggregateSequenceNumber,
                metadata);
        }

        public IDomainEvent Deserialize(string json, IMetadata metadata)
        {
            return Deserialize(metadata.AggregateId, json, metadata);
        }

        public IDomainEvent Deserialize(ICommittedDomainEvent committedDomainEvent)
        {
            var metadata = (IMetadata)_jsonSerializer.Deserialize<Metadata>(committedDomainEvent.Metadata);
            return Deserialize(committedDomainEvent.AggregateId, committedDomainEvent.Data, metadata);
        }

        public IDomainEvent<TAggregate, TIdentity> Deserialize<TAggregate, TIdentity>(
            TIdentity id,
            ICommittedDomainEvent committedDomainEvent)
            where TAggregate : IEventSourced<TIdentity>
            where TIdentity : IIdentity
        {
            return (IDomainEvent<TAggregate, TIdentity>)Deserialize(committedDomainEvent);
        }

        private IDomainEvent Deserialize(string aggregateId, string json, IMetadata metadata)
        {
            var eventDefinition = _eventDefinitionService.GetEventDefinition(
                metadata.EventName,
                metadata.EventVersion);

            var aggregateEvent = (IEvent)_jsonSerializer.Deserialize(json, eventDefinition.Type);

            var domainEvent = _domainEventFactory.Create(
                aggregateEvent,
                metadata,
                aggregateId,
                metadata.AggregateSequenceNumber);

            return domainEvent;
        }
    }
}
