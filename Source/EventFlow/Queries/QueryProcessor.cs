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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.Queries
{
    public class QueryProcessor : IQueryProcessor
    {
        private class CacheItem
        {
            public Type QueryHandlerType { get; set; }
            public Func<IQueryHandler, IQuery, CancellationToken, Task> HandlerFunc { get; set; }
        }

        private readonly ILogger<QueryProcessor> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IMemoryCache _memoryCache;

        public QueryProcessor(
            ILogger<QueryProcessor> logger,
            IServiceProvider serviceProvider,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _memoryCache = memoryCache;
        }

        public async Task<TResult> ProcessAsync<TResult>(
            IQuery<TResult> query,
            CancellationToken cancellationToken)
        {
            var queryType = query.GetType();
            var cacheItem = GetCacheItem(queryType);

            var queryHandler = (IQueryHandler) _serviceProvider.GetRequiredService(cacheItem.QueryHandlerType);
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Executing query {QueryType} ({QueryHandlerType}) by using query handler {QueryHandlerType}",
                    queryType.PrettyPrint(),
                    cacheItem.QueryHandlerType.PrettyPrint(),
                    queryHandler.GetType().PrettyPrint());
            }

            var task = (Task<TResult>) cacheItem.HandlerFunc(queryHandler, query, cancellationToken);

            return await task.ConfigureAwait(false);
        }

        private CacheItem GetCacheItem(
            Type queryType)
        {
            return _memoryCache.GetOrCreate(
                CacheKey.With(GetType(), queryType.GetCacheKey()),
                e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
                        var queryInterfaceType = queryType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IQuery<>));
                        var queryHandlerType = typeof(IQueryHandler<,>).MakeGenericType(queryType, queryInterfaceType.GetTypeInfo().GetGenericArguments()[0]);
                        var invokeExecuteQueryAsync = ReflectionHelper.CompileMethodInvocation<Func<IQueryHandler, IQuery, CancellationToken, Task>>(
                            queryHandlerType,
                            "ExecuteQueryAsync",
                            queryType, typeof(CancellationToken));
                        return new CacheItem
                            {
                                QueryHandlerType = queryHandlerType,
                                HandlerFunc = invokeExecuteQueryAsync
                            };
                    });
        }
    }
}
