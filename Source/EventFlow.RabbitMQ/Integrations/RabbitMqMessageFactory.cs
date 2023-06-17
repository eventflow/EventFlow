// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
// https://github.com/eventflow/EventFlow
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
using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using EventFlow.Commands.Serialization;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using Microsoft.Extensions.Logging;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqMessageFactory : IRabbitMqMessageFactory
    {
        private readonly ILogger<RabbitMqMessageFactory> _log;
        private readonly IEventJsonSerializer _eventJsonSerializer;
        private readonly ICommandJsonSerializer _commmandJsonSerializer;
        private readonly IRabbitMqConfiguration _rabbitMqConfiguration;

        public RabbitMqMessageFactory(
            ILogger<RabbitMqMessageFactory> log,
            IEventJsonSerializer eventJsonSerializer,
            ICommandJsonSerializer commmandJsonSerializer,
            IRabbitMqConfiguration rabbitMqConfiguration)
        {
            _log = log;
            _eventJsonSerializer = eventJsonSerializer;
            _commmandJsonSerializer = commmandJsonSerializer;
            _rabbitMqConfiguration = rabbitMqConfiguration;
        }

        public RabbitMqMessage CreateMessage(IDomainEvent domainEvent)
        {
            var serializedEvent = _eventJsonSerializer.Serialize(
                domainEvent.GetAggregateEvent(),
                domainEvent.Metadata);

            var routingKey = new RoutingKey(string.Format(
                "eventflow.domainevent.{0}.{1}.{2}",
                domainEvent.Metadata[MetadataKeys.AggregateName].ToSlug(),
                domainEvent.Metadata.EventName.ToSlug(),
                domainEvent.Metadata.EventVersion));
            var exchange = new Exchange(_rabbitMqConfiguration.Exchange);

            var rabbitMqMessage = new RabbitMqMessage(
                serializedEvent.SerializedData,
                domainEvent.Metadata,
                exchange,
                routingKey,
                new MessageId(domainEvent.Metadata[MetadataKeys.EventId]));

            _log.LogTrace("Create RabbitMQ message {0}", rabbitMqMessage);

            return rabbitMqMessage;
        }

        public RabbitMqMessage CreateMessage<TAggregate, TIdentity, TExecutionResult>(ICommand<TAggregate, TIdentity, TExecutionResult> applicationCommand)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TExecutionResult : IExecutionResult
        {
            var serializedCommand = _commmandJsonSerializer.Serialize(applicationCommand);
            var metaData = serializedCommand.Metadata;

            var routingKey = new RoutingKey(string.Format(
                "eventflow.applicationcommand.{0}.{1}.{2}",
                metaData.AggregateName.ToSlug(),
                metaData.CommandName.ToSlug(),
                metaData.CommandVersion));

            var exchange = new Exchange(_rabbitMqConfiguration.Exchange);

            var rabbitMqMessage = new RabbitMqMessage(
                serializedCommand.SerializedData,
                metaData,
                exchange,
                routingKey,
                new MessageId(metaData.SourceId.Value));

            _log.LogTrace("Create RabbitMQ message {0}", rabbitMqMessage);

            return rabbitMqMessage;
        }
    }
}