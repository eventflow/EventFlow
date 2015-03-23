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
using System.Globalization;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace EventFlow.EventStores
{
    public class EventJsonSerializer : IEventJsonSerializer
    {
        private readonly IEventDefinitionService _eventDefinitionService;

        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
            {
                ContractResolver = new ContractResolver(),
                Formatting = Formatting.None,
            };

        public class ContractResolver : DefaultContractResolver
        {
            protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
            {
                var jsonProperties = base.CreateProperties(type, memberSerialization)
                    .Where(property => property.DeclaringType != typeof(IAggregateEvent) && property.PropertyName != "Metadata")
                    .ToList();

                return jsonProperties;
            }
        }

        public EventJsonSerializer(
            IEventDefinitionService eventDefinitionService)
        {
            _eventDefinitionService = eventDefinitionService;
        }

        public SerializedEvent Serialize(IAggregateEvent aggregateEvent, IEnumerable<KeyValuePair<string, string>> metadatas)
        {
            var eventDefinition = _eventDefinitionService.GetEventDefinition(aggregateEvent.GetType());

            var metadata = new Metadata(metadatas.Concat(new[]
                {
                    new KeyValuePair<string, string>(MetadataKeys.EventName, eventDefinition.Name),
                    new KeyValuePair<string, string>(MetadataKeys.EventVersion, eventDefinition.Version.ToString(CultureInfo.InvariantCulture)),
                }));

            var dataJson = JsonConvert.SerializeObject(aggregateEvent, Settings);
            var metaJson = JsonConvert.SerializeObject(metadata, Settings);

            return new SerializedEvent(
                metaJson,
                dataJson,
                metadata.AggregateSequenceNumber);
        }

        public IDomainEvent Deserialize(ICommittedDomainEvent committedDomainEvent)
        {
            var metadata = (IMetadata)JsonConvert.DeserializeObject<Metadata>(committedDomainEvent.Metadata);

            var eventDefinition = _eventDefinitionService.GetEventDefinition(
                metadata.EventName,
                metadata.EventVersion);

            var aggregateEvent = (IAggregateEvent)JsonConvert.DeserializeObject(committedDomainEvent.Data, eventDefinition.Type);

            var domainEventType = typeof (DomainEvent<>).MakeGenericType(eventDefinition.Type);
            var domainEvent = (IDomainEvent) Activator.CreateInstance(
                domainEventType,
                aggregateEvent,
                metadata,
                metadata.Timestamp,
                committedDomainEvent.GlobalSequenceNumber,
                committedDomainEvent.AggregateId,
                committedDomainEvent.AggregateSequenceNumber,
                committedDomainEvent.BatchId);

            return domainEvent;
        }
    }
}
