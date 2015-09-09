﻿// The MIT License (MIT)
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
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsEventUpgradersExtensions
    {
        public static EventFlowOptions AddEventUpgrader<TAggregate, TIdentity, TEventUpgrader>(
            this EventFlowOptions eventFlowOptions)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEventUpgrader : class, IEventUpgrader<TAggregate, TIdentity>
        {
            return eventFlowOptions.RegisterServices(f => f.Register<IEventUpgrader<TAggregate, TIdentity>, TEventUpgrader>());
        }

        public static EventFlowOptions AddEventUpgrader<TAggregate, TIdentity>(
            this EventFlowOptions eventFlowOptions,
            Func<IResolverContext, IEventUpgrader<TAggregate, TIdentity>> factory)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return eventFlowOptions.RegisterServices(f => f.Register(factory));
        }

        public static EventFlowOptions AddEventUpgraders(
            this EventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var eventUpgraderTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventUpgrader<,>)))
                .Where(t => predicate(t));
            return eventFlowOptions
                .AddEventUpgraders(eventUpgraderTypes);
        }

        public static EventFlowOptions AddEventUpgraders(
            this EventFlowOptions eventFlowOptions,
            params Type[] eventUpgraderTypes)
        {
            return eventFlowOptions
                .AddEventUpgraders((IEnumerable<Type>)eventUpgraderTypes);
        }

        public static EventFlowOptions AddEventUpgraders(
            this EventFlowOptions eventFlowOptions,
            IEnumerable<Type> eventUpgraderTypes)
        {
            foreach (var eventUpgraderType in eventUpgraderTypes)
            {
                var t = eventUpgraderType;
                var eventUpgraderForAggregateType = t
                    .GetInterfaces()
                    .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IEventUpgrader<,>));
                if (eventUpgraderForAggregateType == null)
                {
                    throw new ArgumentException(string.Format(
                        "Type '{0}' does not have the IEventUpgrader<TAggregate, TIdentity> interface",
                        eventUpgraderType.Name));
                }

                eventFlowOptions.RegisterServices(sr => sr.Register(eventUpgraderForAggregateType, t));
            }

            return eventFlowOptions;
        }
    }
}
