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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;

namespace EventFlow.Sql.Connections
{
    public abstract class SqlConfiguration<T> : ISqlConfiguration<T>
        where T : ISqlConfiguration<T>
    {
        private readonly ConcurrentDictionary<string, string> _connectionStrings = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public RetryDelay TransientRetryDelay { get; private set; } = RetryDelay.Between(
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100));

        public int TransientRetryCount { get; private set; } = 2;
        public TimeSpan UpgradeExecutionTimeout { get; private set; } = TimeSpan.FromMinutes(5);

        public T SetConnectionString(string connectionString)
        {
            if (!_connectionStrings.TryAdd(string.Empty, connectionString))
            {
                throw new ArgumentException("Default connection string already configured");
            }

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetConnectionString(
            string connectionStringName,
            string connectionString)
        {
            if (!_connectionStrings.TryAdd(connectionStringName, connectionString))
            {
                throw new ArgumentException(
                    $"There's already a connection string named '{connectionStringName}'",
                    nameof(connectionStringName));
            }

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetTransientRetryDelay(RetryDelay retryDelay)
        {
            TransientRetryDelay = retryDelay;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetTransientRetryCount(int retryCount)
        {
            TransientRetryCount = retryCount;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetUpgradeExecutionTimeout(TimeSpan timeout)
        {
            UpgradeExecutionTimeout = timeout;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public virtual Task<string> GetConnectionStringAsync(
            Label label,
            string name,
            CancellationToken cancellationToken)
        {
            if (!_connectionStrings.TryGetValue(name ?? string.Empty, out var connectionString))
            {
                throw new ArgumentOutOfRangeException($"There's no connection string named '{name}'");
            }

            return Task.FromResult(connectionString);
        }
    }
}
