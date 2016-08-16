// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;

namespace EventFlow.Core.Caching
{
    public class InMemoryCache : Cache, IInMemoryCache, IDisposable
    {
        private readonly MemoryCache _memoryCache = new MemoryCache($"eventflow-{DateTimeOffset.Now.ToString("yyyyMMdd-HHmm")}-{Guid.NewGuid().ToString("N")}");

        public InMemoryCache(ILog log)
            : base(log)
        {
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }

        protected override Task SetAsync<T>(
            string key,
            DateTimeOffset absoluteExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _memoryCache.Set(
                key,
                value,
                absoluteExpiration);

            return Task.FromResult(0);
        }

        protected override Task SetAsync<T>(
            string key,
            TimeSpan slidingExpiration,
            T value,
            CancellationToken cancellationToken)
        {
            _memoryCache.Set(
                key,
                value,
                new CacheItemPolicy
                {
                    SlidingExpiration = slidingExpiration,
                });

            return Task.FromResult(0);
        }

        protected override Task<T> GetAsync<T>(
            string key,
            CancellationToken cancellationToken)
        {
            var value = _memoryCache.Get(key) as T;

            return Task.FromResult(value);
        }
    }
}