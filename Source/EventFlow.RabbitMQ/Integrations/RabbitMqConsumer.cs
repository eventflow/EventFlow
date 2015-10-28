// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using RabbitMQ.Client.Events;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqConsumer<THandler> : IRabbitMqConsumer<THandler>
        where THandler : IRabbitMqMessageHandler
    {
        private readonly ILog _log;
        private readonly THandler _messageHandler;
        private readonly IRabbitMqConnectionFactory _connectionFactory;

        public RabbitMqConsumer(
            ILog log,
            THandler messageHandler,
            IRabbitMqConnectionFactory connectionFactory)
        {
            _log = log;
            _messageHandler = messageHandler;
            _connectionFactory = connectionFactory;
        }

        public Task StopAsync()
        {
            return Task.FromResult(0);
        }

        public async Task StartAsync()
        {
            var connection = await _connectionFactory.CreateConnectionAsync(new Uri(""), CancellationToken.None).ConfigureAwait(false);
            var eventingBasicConsumer = await connection.WithModelAsync(m => Task.FromResult(new EventingBasicConsumer(m)), CancellationToken.None).ConfigureAwait(false);
            eventingBasicConsumer.Received += EventingBasicConsumerOnReceived;
        }

        private void EventingBasicConsumerOnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            var rabbitMqMessage = RabbitMqMessage.Create(basicDeliverEventArgs);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(_messageHandler.HandleAsync(rabbitMqMessage, CancellationToken.None));
            }
        }

        public void Dispose()
        {
        }
    }
}