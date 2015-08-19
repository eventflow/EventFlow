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

using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqMessageFactory : IRabbitMqMessageFactory
    {
        private readonly ILog _log;
        private readonly IEventJsonSerializer _eventJsonSerializer;

        public RabbitMqMessageFactory(
            ILog log,
            IEventJsonSerializer eventJsonSerializer)
        {
            _log = log;
            _eventJsonSerializer = eventJsonSerializer;
        }

        public RabbitMqMessage CreateMessage(IDomainEvent domainEvent)
        {
            var serializedEvent = _eventJsonSerializer.Serialize(
                domainEvent.GetAggregateEvent(),
                domainEvent.Metadata);

            // TODO: Add aggregate name to routing key

            var routingKey = new RoutingKey(string.Format(
                "eventflow.domainevent.{0}.{1}.{2}",
                domainEvent.Metadata[MetadataKeys.AggregateName].ToSlug(),
                domainEvent.Metadata.EventName.ToSlug(),
                domainEvent.Metadata.EventVersion));
            var exchange = new Exchange("eventflow");

            var rabbitMqMessage = new RabbitMqMessage(
                serializedEvent.SerializedData,
                domainEvent.Metadata,
                exchange,
                routingKey);

            _log.Verbose("Create RabbitMQ message {0}", rabbitMqMessage);

            return rabbitMqMessage;
        }
    }
}
