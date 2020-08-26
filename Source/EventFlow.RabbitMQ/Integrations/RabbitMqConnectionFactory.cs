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
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using RabbitMQ.Client;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqConnectionFactory : IRabbitMqConnectionFactory
    {
        private readonly ILog _log;
        private readonly IRabbitMqConfiguration _configuration;
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly Dictionary<Uri, ConnectionFactory> _connectionFactories = new Dictionary<Uri, ConnectionFactory>();

        public RabbitMqConnectionFactory(
            ILog log,
            IRabbitMqConfiguration configuration)
        {
            _log = log;
            _configuration = configuration;
        }

        public async Task<IRabbitConnection> CreateConnectionAsync(Uri uri, CancellationToken cancellationToken)
        {
            var connectionFactory = await CreateConnectionFactoryAsync(uri, cancellationToken).ConfigureAwait(false);
            var connection = connectionFactory.CreateConnection();

            return new RabbitConnection(_log, _configuration.ModelsPrConnection, connection);
        }

        private async Task<ConnectionFactory> CreateConnectionFactoryAsync(Uri uri, CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                ConnectionFactory connectionFactory;
                if (_connectionFactories.TryGetValue(uri, out connectionFactory))
                {
                    return connectionFactory;
                }
                _log.Verbose("Creating RabbitMQ connection factory to {0}", uri.Host);

                connectionFactory = new ConnectionFactory
                    {
                        Uri = uri,
                        UseBackgroundThreadsForIO = true, // TODO: As soon as RabbitMQ supports async/await, set to false
                        TopologyRecoveryEnabled = true,
                        AutomaticRecoveryEnabled = true,
                        ClientProperties = new Dictionary<string, object>
                            {
                                { "eventflow-version", typeof(RabbitMqConnectionFactory).GetTypeInfo().Assembly.GetName().Version.ToString() },
                                { "machine-name", Environment.MachineName },
                            },
                    };

                _connectionFactories.Add(uri, connectionFactory);
                return connectionFactory;
            }
        }
    }
}