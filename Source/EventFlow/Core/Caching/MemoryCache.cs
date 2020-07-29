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

#if NET452

using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;

namespace EventFlow.Core.Caching
{
    public class MemoryCache : Cache, IMemoryCache, IDisposable
    {
        private readonly System.Runtime.Caching.MemoryCache _memoryCache = new System.Runtime.Caching.MemoryCache(GenerateKey());

        private static string GenerateKey()
        {
            return $"eventflow-{DateTimeOffset.Now:yyyyMMdd-HHmm}-{Guid.NewGuid():N}";
        }

        public MemoryCache(ILog log)
            : base(log)
        {
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        protected override Task SetAsync<T>(
            CacheKey cacheKey,
            DateTimeOffset absoluteExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _memoryCache.Set(
                cacheKey.Value,
                value,
                absoluteExpiration);

            return Task.FromResult(0);
        }

        protected override Task SetAsync<T>(
            CacheKey cacheKey,
            TimeSpan slidingExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _memoryCache.Set(
                cacheKey.Value,
                value,
                new CacheItemPolicy
                {
                    SlidingExpiration = slidingExpiration,
                });

            return Task.FromResult(0);
        }

        protected override Task<T> GetAsync<T>(
            CacheKey cacheKey,
            CancellationToken cancellationToken)
        {
            var value = _memoryCache.Get(cacheKey.Value) as T;

            return Task.FromResult(value);
        }
    }
}

#endif