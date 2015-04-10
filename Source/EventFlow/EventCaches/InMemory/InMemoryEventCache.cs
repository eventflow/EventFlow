// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.EventCaches.InMemory
{
    public class InMemoryEventCache : IEventCache, IDisposable
    {
        private static readonly TimeSpan CacheTime = TimeSpan.FromMinutes(5);
        private readonly ILog _log;
        private readonly MemoryCache _memoryCache = new MemoryCache(string.Format(
            "{0}-{1}",
            typeof(InMemoryEventCache).FullName,
            Guid.NewGuid())); 

        public InMemoryEventCache(
            ILog log)
        {
            _log = log;
        }

        public Task InsertAsync(
            Type aggregateType,
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            if (domainEvents == null) throw new ArgumentNullException("domainEvents");
            if (!domainEvents.Any()) throw new ArgumentException(string.Format(
                "You must provide events to cache for aggregate '{0}' with ID '{1}'",
                aggregateType.Name,
                id));

            var cacheKey = GetKey(aggregateType, id);
            _memoryCache.Set(cacheKey, domainEvents, DateTimeOffset.Now.Add(CacheTime));
            _log.Verbose(
                "Added cache key {0} with {1} events to in-memory event store cache. Now it has {2} streams cached.",
                cacheKey,
                domainEvents.Count,
                _memoryCache.GetCount());
            return Task.FromResult(0);
        }

        public Task InvalidateAsync(
            Type aggregateType,
            string id,
            CancellationToken cancellationToken)
        {
            var cacheKey = GetKey(aggregateType, id);
            if (_memoryCache.Contains(cacheKey))
            {
                _log.Verbose(
                    "Found and invalidated in-memory cache for aggregate '{0}' with ID '{1}'",
                    aggregateType.Name,
                    id);
                _memoryCache.Remove(cacheKey);
            }

            return Task.FromResult(0);
        }

        public Task<IReadOnlyCollection<IDomainEvent>> GetAsync(
            Type aggregateType,
            string id,
            CancellationToken cancellationToken)
        {
            var cacheKey = GetKey(aggregateType, id);
            var domainEvents = _memoryCache.Get(cacheKey) as IReadOnlyCollection<IDomainEvent>;
            if (domainEvents == null)
            {
                _log.Verbose(
                    "Didn't not find anything in in-memory cache for aggregate '{0}' with ID '{1}'",
                    aggregateType.Name,
                    id);
            }
            else
            {
                _log.Verbose(
                    "Found {0} events in in-memory cache for aggregate '{1}' with ID '{2}'",
                    domainEvents.Count,
                    aggregateType.Name,
                    id);
            }

            return Task.FromResult(domainEvents);
        }

        private static string GetKey(Type aggregateType, string id)
        {
            return string.Format("{0} ({1})", aggregateType.FullName, id);
        }

        public void Dispose()
        {
            _memoryCache.Dispose();
        }
    }
}
