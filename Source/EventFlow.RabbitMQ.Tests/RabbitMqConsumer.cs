// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EventFlow.RabbitMQ.Integrations;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventFlow.RabbitMQ.Tests
{
    public class RabbitMqConsumer : IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _model;
        private readonly EventingBasicConsumer _eventingBasicConsumer;
        private readonly BlockingCollection<BasicDeliverEventArgs> _receivedMessages = new BlockingCollection<BasicDeliverEventArgs>(); 

        public RabbitMqConsumer(Uri uri, Exchange exchange, IEnumerable<string> routingKeys)
        {
            var connectionFactory = new ConnectionFactory
                {
                    Uri = uri,
                };
            _connection = connectionFactory.CreateConnection();
            _model = _connection.CreateModel();

            _model.ExchangeDeclare(exchange.Value, ExchangeType.Topic, false);

            var queueName = $"test-{Guid.NewGuid():N}";
            _model.QueueDeclare(
                queueName,
                false,
                false,
                true,
                null);

            foreach (var routingKey in routingKeys)
            {
                _model.QueueBind(
                    queueName,
                    exchange.Value,
                    routingKey,
                    null);
            }

            _eventingBasicConsumer = new EventingBasicConsumer(_model);
            _eventingBasicConsumer.Received += OnReceived;

            _model.BasicConsume(queueName, false, _eventingBasicConsumer);
        }

        private void OnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            _receivedMessages.Add(basicDeliverEventArgs);
        }

        public IReadOnlyCollection<RabbitMqMessage> GetMessages(TimeSpan timeout, int count = 1)
        {
            var rabbitMqMessages = new List<RabbitMqMessage>();
            var stopwatch = Stopwatch.StartNew();

            while (rabbitMqMessages.Count < count)
            {
                if (stopwatch.Elapsed >= timeout)
                {
                    throw new TimeoutException($"Timed out after {stopwatch.Elapsed.TotalSeconds:0.##} seconds");
                }

                if (!_receivedMessages.TryTake(out var basicDeliverEventArgs, TimeSpan.FromMilliseconds(100)))
                {
                    continue;
                }

                rabbitMqMessages.Add(CreateRabbitMqMessage(basicDeliverEventArgs));
            }

            return rabbitMqMessages;
        }

        private static RabbitMqMessage CreateRabbitMqMessage(BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var headers = basicDeliverEventArgs.BasicProperties.Headers
                .ToDictionary(kv => kv.Key, kv => Encoding.UTF8.GetString((byte[])kv.Value));
            var message = Encoding.UTF8.GetString(basicDeliverEventArgs.Body);

            return new RabbitMqMessage(
                message,
                headers,
                new Exchange(basicDeliverEventArgs.Exchange), 
                new RoutingKey(basicDeliverEventArgs.RoutingKey),
                new MessageId(basicDeliverEventArgs.BasicProperties.MessageId));
        }

        public void Dispose()
        {
            _eventingBasicConsumer.Received -= OnReceived;
            _model.Dispose();
            _connection.Dispose();
            _receivedMessages.Dispose();
        }
    }
}