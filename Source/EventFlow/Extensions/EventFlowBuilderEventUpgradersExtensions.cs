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
using EventFlow.EventStores;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Extensions
{
    public static class EventFlowBuilderEventUpgradersExtensions
    {
        public static IEventFlowBuilder AddEventUpgrader<TAggregate, TIdentity, TEventUpgrader>(
            this IEventFlowBuilder eventFlowBuilder)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
            where TEventUpgrader : class, IEventUpgrader<TAggregate, TIdentity>
        {
            eventFlowBuilder.Services
                .AddTransient<IEventUpgrader<TAggregate, TIdentity>, TEventUpgrader>();

            return eventFlowBuilder;
        }

        public static IEventFlowBuilder AddEventUpgraders(
            this IEventFlowBuilder eventFlowBuilder,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var eventUpgraderTypes = fromAssembly
                .GetTypes()
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsEventUpgraderInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsEventUpgraderInterface))
                .Where(t => predicate(t));
            return eventFlowBuilder
                .AddEventUpgraders(eventUpgraderTypes);
        }

        public static IEventFlowBuilder AddEventUpgraders(
            this IEventFlowBuilder eventFlowBuilder,
            params Type[] eventUpgraderTypes)
        {
            return eventFlowBuilder
                .AddEventUpgraders((IEnumerable<Type>)eventUpgraderTypes);
        }

        public static IEventFlowBuilder AddEventUpgraders(
            this IEventFlowBuilder eventFlowBuilder,
            IEnumerable<Type> eventUpgraderTypes)
        {
            var serviceRegistry = eventFlowBuilder.Services;
            foreach (var eventUpgraderType in eventUpgraderTypes)
            {
                var t = eventUpgraderType;
                if (t.GetTypeInfo().IsAbstract)
                {
                    continue;
                }

                var eventUpgraderForAggregateType = t
                    .GetTypeInfo()
                    .GetInterfaces()
                    .SingleOrDefault(IsEventUpgraderInterface);
                if (eventUpgraderForAggregateType == null)
                {
                    throw new ArgumentException($"Type '{eventUpgraderType.Name}' does not have the '{typeof(IEventUpgrader<,>).PrettyPrint()}' interface");
                }

                serviceRegistry.AddTransient(eventUpgraderForAggregateType, t);
            }

            return eventFlowBuilder;
        }

        private static bool IsEventUpgraderInterface(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEventUpgrader<,>);
        }
    }
}
