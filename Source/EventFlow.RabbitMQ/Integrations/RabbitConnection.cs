// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitConnection : IRabbitConnection
    {
        private readonly ILogger<RabbitConnection> _log;
        private readonly IConnection _connection;
        private readonly AsyncLock _asyncLock;

#if NET8_0_OR_GREATER
        private readonly ConcurrentBag<IChannel> _models;
#else
        private readonly ConcurrentBag<IModel> _models;
#endif

#if NET8_0_OR_GREATER
        public RabbitConnection(ILogger<RabbitConnection> log, IReadOnlyList<IChannel> models, int maxModels, IConnection connection)
        {
            _connection = connection;
            _log = log;
            _asyncLock = new AsyncLock(maxModels);
            _models = new ConcurrentBag<IChannel>(models);
        }
#else
        public RabbitConnection(ILogger<RabbitConnection> log, IEnumerable<IModel> models, int maxModels, IConnection connection)
        {
            _connection = connection;
            _log = log;
            _asyncLock = new AsyncLock(maxModels);
            _models = new ConcurrentBag<IModel>(models);
        }
#endif

#if NET8_0_OR_GREATER
        public async Task<int> WithModelAsync(Func<IChannel, Task> action, CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                IChannel model;
                if (!_models.TryTake(out model))
                {
                    throw new InvalidOperationException(
                        "This should NEVER happen! If it does, please report a bug.");
                }

                try
                {
                    await action(model).ConfigureAwait(false);
                }
                finally
                {
                    _models.Add(model);
                }
            }

            return 0;
        }
#else
        public async Task<int> WithModelAsync(Func<IModel, Task> action, CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                IModel model;
                if (!_models.TryTake(out model))
                {
                    throw new InvalidOperationException(
                        "This should NEVER happen! If it does, please report a bug.");
                }

                try
                {
                    await action(model).ConfigureAwait(false);
                }
                finally
                {
                    _models.Add(model);
                }
            }

            return 0;
        }
#endif


        public void Dispose()
        {
            foreach (var model in _models)
            {
                model.DisposeSafe(_log, "Failed to dispose model");
            }
            _connection.DisposeSafe(_log, "Failed to dispose connection");
            _log.LogTrace("Disposing RabbitMQ connection");
        }
    }
}