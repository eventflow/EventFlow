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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Core.Caching
{
    public abstract class Cache
    {
        protected ILog Log { get; }

        protected Cache(
            ILog log)
        {
            Log = log;
        }

        public Task<T> GetOrAddAsync<T>(
            CacheKey cacheKey,
            TimeSpan slidingExpiration,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken cancellationToken)
            where T : class
        {
            return GetOrAddAsync(
                cacheKey,
                factory,
                (k, v, c) => SetAsync(k, slidingExpiration, v, c),
                cancellationToken);
        }

        public Task<T> GetOrAddAsync<T>(
            CacheKey cacheKey,
            DateTimeOffset expirationTime,
            Func<CancellationToken, Task<T>> factory,
            CancellationToken cancellationToken)
            where T : class
        {
            return GetOrAddAsync(
                cacheKey,
                factory,
                (k, v, c) => SetAsync(k, expirationTime, v, c),
                cancellationToken);
        }

        private async Task<T> GetOrAddAsync<T>(
            CacheKey cacheKey,
            Func<CancellationToken, Task<T>> factory,
            Func<CacheKey, T, CancellationToken, Task> setter,
            CancellationToken cancellationToken)
            where T : class
        {
            if (cacheKey == null) throw new ArgumentNullException(nameof(cacheKey));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            T value;

            try
            {
                value = await GetAsync<T>(cacheKey, cancellationToken).ConfigureAwait(false);
                if (value != null)
                {
                    return value;
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to get '{cacheKey}' from '{GetType().PrettyPrint()}' cache due to unexpected exception");
            }

            value = await factory(cancellationToken).ConfigureAwait(false);
            if (value == null)
            {
                throw new InvalidOperationException(
                    $"Cache factory method for key '{cacheKey}' of type '{typeof(T).PrettyPrint()}' in cache '{GetType().PrettyPrint()}' must not return 'null'");
            }

            try
            {
                await setter(cacheKey, value, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Warning(e, $"Failed to set '{cacheKey}' in '{GetType().PrettyPrint()}' cache due to unexpected exception");
            }

            return value;
        }

        protected abstract Task SetAsync<T>(
            CacheKey cacheKey,
            DateTimeOffset absoluteExpiration,
            T value,
            CancellationToken cancellationToken)
            where T : class;

        protected abstract Task SetAsync<T>(
            CacheKey cacheKey,
            TimeSpan slidingExpiration,
            T value,
            CancellationToken cancellationToken)
            where T : class;

        protected abstract Task<T> GetAsync<T>(
            CacheKey cacheKey,
            CancellationToken cancellationToken)
            where T : class;
    }
}