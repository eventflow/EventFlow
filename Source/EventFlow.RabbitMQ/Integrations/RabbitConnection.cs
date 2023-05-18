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

using System;
using System.Collections.Concurrent;
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
        private readonly ILogger _logger;
        private readonly IConnection _connection;
        private readonly AsyncLock _asyncLock;
        private readonly ConcurrentBag<IModel> _models; 

        public RabbitConnection(
            ILogger logger,
            int maxModels,
            IConnection connection)
        {
            _logger = logger;
            _connection = connection;
            _asyncLock = new AsyncLock(maxModels);
            _models = new ConcurrentBag<IModel>(Enumerable.Range(0, maxModels).Select(_ => connection.CreateModel()));
        }

        public async Task<int> WithModelAsync(Func<IModel, Task> action, CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                if (!_models.TryTake(out var model))
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

        public void Dispose()
        {
            _logger.LogTrace("Disposing RabbitMQ connection");
            foreach (var model in _models)
            {
                model.DisposeSafe(_logger, "Failed to dispose model");
            }
            _connection.DisposeSafe(_logger, "Failed to dispose connection");
        }
    }
}