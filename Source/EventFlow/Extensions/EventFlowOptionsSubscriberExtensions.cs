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
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Subscribers;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsSubscriberExtensions
    {
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

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            params Type[] subscribeSynchronousToTypes)
        {
            return eventFlowOptions.AddSubscribers((IEnumerable<Type>) subscribeSynchronousToTypes);
        }

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var subscribeSynchronousToTypes = fromAssembly
                .GetTypes()
                .Where(t => t
                    .GetInterfaces()
                    .Any(i =>
                        (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISubscribeSynchronousTo<,,>)) ||
                        i == typeof(ISubscribeSynchronousToAll)))
                .Where(t => predicate(t));
            return eventFlowOptions.AddSubscribers(subscribeSynchronousToTypes);
        }

        public static IEventFlowOptions AddSubscribers(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> subscribeSynchronousToTypes)
        {
            foreach (var subscribeSynchronousToType in subscribeSynchronousToTypes)
            {
                var t = subscribeSynchronousToType;
                var subscribeTos = t
                    .GetInterfaces()
                    .Where(i =>
                        (i.IsGenericType && i.GetGenericTypeDefinition() == typeof (ISubscribeSynchronousTo<,,>)) ||
                        i == typeof(ISubscribeSynchronousToAll))
                    .ToList();
                if (!subscribeTos.Any())
                {
                    throw new ArgumentException($"Type '{t.PrettyPrint()}' is not an '{typeof(ISubscribeSynchronousTo<,,>).PrettyPrint()}'");
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
    }
}
