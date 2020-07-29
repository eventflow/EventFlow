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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Queries;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsQueriesExtensions
    {
        public static IEventFlowOptions AddQueryHandler<TQueryHandler, TQuery, TResult>(
            this IEventFlowOptions eventFlowOptions)
            where TQueryHandler : class, IQueryHandler<TQuery, TResult>
            where TQuery : IQuery<TResult>
        {
            return eventFlowOptions.RegisterServices(sr => sr.Register<IQueryHandler<TQuery, TResult>, TQueryHandler>());
        }

        public static IEventFlowOptions AddQueryHandlers(
            this IEventFlowOptions eventFlowOptions,
            params Type[] queryHandlerTypes)
        {
            return eventFlowOptions.AddQueryHandlers((IEnumerable<Type>)queryHandlerTypes);
        }

        public static IEventFlowOptions AddQueryHandlers(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var subscribeSynchronousToTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsQueryHandlerInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsQueryHandlerInterface))
                .Where(t => predicate(t));
            return eventFlowOptions
                .AddQueryHandlers(subscribeSynchronousToTypes);
        }

        public static IEventFlowOptions AddQueryHandlers(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> queryHandlerTypes)
        {
            foreach (var queryHandlerType in queryHandlerTypes)
            {
                var t = queryHandlerType;
                if (t.GetTypeInfo().IsAbstract) continue;
                var queryHandlerInterfaces = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsQueryHandlerInterface)
                    .ToList();
                if (!queryHandlerInterfaces.Any())
                {
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{typeof(IQueryHandler<,>).PrettyPrint()}'");
                }

                eventFlowOptions.RegisterServices(sr =>
                    {
                        foreach (var queryHandlerInterface in queryHandlerInterfaces)
                        {
                            sr.Register(queryHandlerInterface, t);
                        }
                    });
            }

            return eventFlowOptions;
        }

        private static bool IsQueryHandlerInterface(this Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryHandler<,>);
        }
    }
}