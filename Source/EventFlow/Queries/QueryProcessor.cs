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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Queries
{
    public class QueryProcessor : IQueryProcessor
    {
        private class CacheItem
        {
            public Type QueryHandlerType { get; set; }
            public Func<object, object, CancellationToken, object> HandlerFunc { get; set; }
        }

        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly ConcurrentDictionary<Type, CacheItem> _cacheItems = new ConcurrentDictionary<Type, CacheItem>(); 

        public QueryProcessor(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public async Task<TResult> ProcessAsync<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        {
            var queryType = query.GetType();
            var cacheItem = _cacheItems.GetOrAdd(
                queryType,
                CreateCacheItem);

            var queryHandler = _resolver.Resolve(cacheItem.QueryHandlerType);
            _log.Verbose(() => $"Executing query '{queryType.PrettyPrint()}' by using query handler '{cacheItem.QueryHandlerType.PrettyPrint()}'");
            
            var task = (Task<TResult>) cacheItem.HandlerFunc(queryHandler, query, cancellationToken);

            return await task.ConfigureAwait(false);
        }

        public TResult Process<TResult>(IQuery<TResult> query, CancellationToken cancellationToken)
        {
            var result = default(TResult);
            using (var a = AsyncHelper.Wait)
            {
                a.Run(ProcessAsync(query, cancellationToken), r => result = r);
            }
            return result;
        }

        private static CacheItem CreateCacheItem(Type queryType)
        {
            var queryInterfaceType = queryType
                .GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IQuery<>));
            var queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, queryInterfaceType.GetGenericArguments()[0]);
            var methodInfo = queryHandlerType.GetMethod("ExecuteQueryAsync");
            return new CacheItem
                {
                    QueryHandlerType = queryHandlerType,
                    HandlerFunc = (h, q, c) => methodInfo.Invoke(h, new object[]{q, c})
                };
        }
    }
}
