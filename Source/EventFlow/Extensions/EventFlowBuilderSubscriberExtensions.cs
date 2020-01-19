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
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Extensions
{
    public static class EventFlowBuilderSubscriberExtensions
    {
        private static readonly Type SubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type SubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);
        private static readonly Type SubscribeSynchronousToAllType = typeof(ISubscribeSynchronousToAll);

        public static IEventFlowBuilder AddSynchronousSubscriber<TAggregate, TIdentity, TEvent, TSubscriber>(
            this IEventFlowBuilder eventFlowBuilder)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
            where TSubscriber : class, ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>
        {
            eventFlowBuilder.Services
                .AddTransient<ISubscribeSynchronousTo<TAggregate, TIdentity, TEvent>, TSubscriber>();
            return eventFlowBuilder;
        }

        public static IEventFlowBuilder AddAsynchronousSubscriber<TAggregate, TIdentity, TEvent, TSubscriber>(
            this IEventFlowBuilder eventFlowBuilder)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEvent : IAggregateEvent<TAggregate, TIdentity>
            where TSubscriber : class, ISubscribeAsynchronousTo<TAggregate, TIdentity, TEvent>
        {
            eventFlowBuilder.Services
                .AddTransient<ISubscribeAsynchronousTo<TAggregate, TIdentity, TEvent>, TSubscriber>();
            return eventFlowBuilder;
        }

        public static IEventFlowBuilder AddSubscribers(
            this IEventFlowBuilder eventFlowBuilder,
            params Type[] types)
        {
            return eventFlowBuilder.AddSubscribers((IEnumerable<Type>) types);
        }

        public static IEventFlowBuilder AddSubscribers(
            this IEventFlowBuilder eventFlowBuilder,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var types = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsSubscriberInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsSubscriberInterface))
                .Where(t => predicate(t));
            return eventFlowBuilder.AddSubscribers(types);
        }

        public static IEventFlowBuilder AddSubscribers(
            this IEventFlowBuilder eventFlowBuilder,
            IEnumerable<Type> subscribeSynchronousToTypes)
        {
            var serviceCollection = eventFlowBuilder.Services;
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
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{SubscribeSynchronousToType.PrettyPrint()}', '{SubscribeAsynchronousToType.PrettyPrint()}' or '{SubscribeSynchronousToAllType.PrettyPrint()}'");
                }

                foreach (var subscribeTo in subscribeTos)
                {
                    serviceCollection.AddTransient(subscribeTo, t);
                }
            }

            return eventFlowBuilder;
        }

        private static bool IsSubscriberInterface(Type type)
        {
            if (type == SubscribeSynchronousToAllType)
            {
                return true;
            }

            var typeInfo = type.GetTypeInfo();
            if (!typeInfo.IsGenericType)
            {
                return false;
            }

            var genericTypeDefinition = type.GetGenericTypeDefinition();

            return genericTypeDefinition == SubscribeSynchronousToType ||
                   genericTypeDefinition == SubscribeAsynchronousToType;
        }
    }
}
