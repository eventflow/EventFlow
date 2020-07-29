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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;

namespace EventFlow.Core.Caching
{
    /// <summary>
    /// Simple cache that disregards expiration times and keeps everything forever,
    /// useful when doing tests.
    /// </summary>
    public class DictionaryMemoryCache : Cache, IMemoryCache
    {
        private readonly ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

        public DictionaryMemoryCache(
            ILog log)
            : base(log)
        {
        }

        protected override Task SetAsync<T>(
            CacheKey cacheKey,
            DateTimeOffset absoluteExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _cache[cacheKey.Value] = value;

            return Task.FromResult(0);
        }

        protected override Task SetAsync<T>(
            CacheKey cacheKey,
            TimeSpan slidingExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _cache[cacheKey.Value] = value;

            return Task.FromResult(0);
        }

        protected override Task<T> GetAsync<T>(
            CacheKey cacheKey,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_cache.TryGetValue(cacheKey.Value, out var value)
                ? value as T
                : default(T));
        }
    }
}