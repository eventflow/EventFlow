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
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Subscribers;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsSubscriberExtensions
    {
        private static readonly Type ISubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type ISubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);
        private static readonly Type ISubscribeSynchronousToAllType = typeof(ISubscribeSynchronousToAll);

        [Obsolete("Please use the more explicit method 'AddSynchronousSubscriber<,,,>' instead")]
        public static IEventFlowOptions AddSubscriber<TAggregate, TIdentity, TEvent, TSubscriber>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
            where TSubscriber : class, ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>
        {
            return eventFlowOptions
                .RegisterServices(sr => sr.Register<ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>, TSubscriber>());
        }

        public static IEventFlowOptions AddSynchronousSubscriber<TAggregate, TIdentity, TEvent, TSubscriber>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
            where TSubscriber : class, ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>
        {
            return eventFlowOptions
                .RegisterServices(sr => sr.Register<ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>, TSubscriber>());
        }

        public static IEventFlowOptions AddAsynchronousSubscriber<TAggregate, TIdentity, TEvent, TSubscriber>(
            this IEventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
            where TSubscriber : class, ISubscribeAsynchronousTo<TAggregate, TIdentity, TEvent>
        {
            return eventFlowOptions
                .RegisterServices(sr => sr.Register<ISubscribeAsynchronousTo<TAggregate, TIdentity, TEvent>, TSubscriber>());
        }

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            params Type[] types)
        {
            return eventFlowOptions.AddSubscribers((IEnumerable<Type>) types);
        }

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var types = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsSubscriberInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsSubscriberInterface))
                .Where(t => predicate(t));
            return eventFlowOptions.AddSubscribers(types);
        }

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> subscribeSynchronousToTypes)
        {
            foreach (var subscribeSynchronousToType in subscribeSynchronousToTypes)
            {
                var t = subscribeSynchronousToType;
                if (t.GetTypeInfo().IsAbstract)
                {
                    continue;
                }

                var subscribeTos = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .Where(IsSubscriberInterface)
                    .ToList();
                if (!subscribeTos.Any())
                {
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{ISubscribeSynchronousToType.PrettyPrint()}', '{ISubscribeAsynchronousToType.PrettyPrint()}' or '{ISubscribeSynchronousToAllType.PrettyPrint()}'");
                }

                eventFlowOptions.RegisterServices(sr =>
                    {
                        foreach (var subscribeTo in subscribeTos)
                        {
                            sr.Register(subscribeTo, t);
                        }
                    });
            }

            return eventFlowOptions;
        }

        private static bool IsSubscriberInterface(Type type)
        {
            if (type == ISubscribeSynchronousToAllType)
            {
                return true;
            }

            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return false;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();

            return genericTypeDefinition == ISubscribeSynchronousToType ||
                   genericTypeDefinition == ISubscribeAsynchronousToType;
        }
    }
}
