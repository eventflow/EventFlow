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
using EventFlow.Extensions;
using EventFlow.Logs;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqConsumer<THandler> : IRabbitMqConsumer<THandler>
        where THandler : IRabbitMqMessageHandler
    {
        private readonly ILog _log;
        private readonly IRabbitMqConfiguration _configuration;
        private readonly THandler _messageHandler;
        private readonly IRabbitMqConnectionFactory _connectionFactory;
        private readonly AsyncLock _asyncLock = new AsyncLock();

        private IConnection _connection;
        private IModel _model;
        private EventingBasicConsumer _eventingBasicConsumer;

        public RabbitMqConsumer(
            ILog log,
            IRabbitMqConfiguration configuration,
            THandler messageHandler,
            IRabbitMqConnectionFactory connectionFactory)
        {
            _log = log;
            _configuration = configuration;
            _messageHandler = messageHandler;
            _connectionFactory = connectionFactory;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return DisposeConnectionAsync(cancellationToken);
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return ReconnectHandlerAsync(cancellationToken);
        }

        private async Task ReconnectHandlerAsync(CancellationToken cancellationToken)
        {
            // TODO: Clean up this mess
            // TODO: Detect a broken connection

            var isReconnecting = false;
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_connection == null || !_connection.IsOpen)
                {
                    _connection?.DisposeSafe(_log, "Could not dispose RabbitMQ connection");
                    _connection = await _connectionFactory.CreateConnectionAsync(_configuration.Uri, cancellationToken).ConfigureAwait(false);
                    isReconnecting = true;
                }

                if (_model == null || _model.IsClosed || isReconnecting)
                {
                    _model?.DisposeSafe(_log, "Could not dispose RabbitMQ model");
                    _model = _connection.CreateModel();
                    isReconnecting = true;
                }

                if (!isReconnecting)
                {
                    return;
                }

                if (_eventingBasicConsumer != null)
                {
                    _eventingBasicConsumer.Received -= EventingBasicConsumerOnReceived;
                }

                _eventingBasicConsumer = new EventingBasicConsumer(_model);
                _eventingBasicConsumer.Received += EventingBasicConsumerOnReceived;
            }
        }

        private async Task DisposeConnectionAsync(CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                _model?.DisposeSafe(_log, "Could not dispose RabbitMQ model");
                _connection?.DisposeSafe(_log, "Could not dispose RabbitMQ connection");
            }
        }

        private void EventingBasicConsumerOnReceived(object sender, BasicDeliverEventArgs basicDeliverEventArgs)
        {
            // TODO: Wait for message handler to finish before shutting down

            var rabbitMqMessage = RabbitMqMessage.Create(basicDeliverEventArgs);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(_messageHandler.HandleAsync(rabbitMqMessage, CancellationToken.None));
            }
        }

        public void Dispose()
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(DisposeConnectionAsync(CancellationToken.None));
            }
        }
    }
}