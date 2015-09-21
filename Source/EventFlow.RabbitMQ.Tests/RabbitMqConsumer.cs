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
using System.Linq;
using System.Text;
using System.Threading;
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
        private readonly List<BasicDeliverEventArgs> _receivedMessages = new List<BasicDeliverEventArgs>(); 
        private readonly AutoResetEvent _autoResetEvent = new AutoResetEvent(false);

        public RabbitMqConsumer(Uri uri, string exchange, IEnumerable<string> routingKeys)
        {
            var connectionFactory = new ConnectionFactory
                {
                    Uri = uri.ToString(),
                };
            _connection = connectionFactory.CreateConnection();
            _model = _connection.CreateModel();

            _model.ExchangeDeclare(exchange, ExchangeType.Topic, false);

            var queueName = string.Format("test-{0}", Guid.NewGuid());
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
                    exchange,
                    routingKey,
                    null);
            }

            _eventingBasicConsumer = new EventingBasicConsumer(_model);
            _eventingBasicConsumer.Received += OnReceived;

            _model.BasicConsume(queueName, false, _eventingBasicConsumer);
        }

        private void OnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            lock (_receivedMessages)
            {
                _receivedMessages.Add(basicDeliverEventArgs);
                _autoResetEvent.Set();
            }
        }

        public IReadOnlyCollection<RabbitMqMessage> GetMessages(int count = 1)
        {
            while (true)
            {
                _autoResetEvent.WaitOne();
                lock (_receivedMessages)
                {
                    if (_receivedMessages.Count >= count)
                    {
                        var basicDeliverEventArgses =_receivedMessages.GetRange(0, count);
                        _receivedMessages.RemoveRange(0, count);
                        return basicDeliverEventArgses.Select(RabbitMqMessage.Create).ToList();
                    }
                }
            }
        }

        public void Dispose()
        {
            _eventingBasicConsumer.Received -= OnReceived;
            _model.Dispose();
            _connection.Dispose();
        }
    }
}
