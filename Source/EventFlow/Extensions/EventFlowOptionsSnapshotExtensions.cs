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
using EventFlow.Configuration;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using EventFlow.Snapshots.Stores.InMemory;

namespace EventFlow.Extensions
{
    public static class EventFlowOptionsSnapshotExtensions
    {
        public static IEventFlowOptions AddSnapshots(
            this IEventFlowOptions eventFlowOptions,
            params Type[] snapshotTypes)
        {
            return eventFlowOptions.AddSnapshots(snapshotTypes);
        }

        public static IEventFlowOptions AddSnapshots(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);
            var snapshotTypes = fromAssembly
                .GetTypes()
                .Where(t => !t.GetTypeInfo().IsAbstract && typeof(ISnapshot).GetTypeInfo().IsAssignableFrom(t))
                .Where(t => predicate(t));
            return eventFlowOptions.AddSnapshots(snapshotTypes);
        }

        public static IEventFlowOptions AddSnapshotUpgraders(
            this IEventFlowOptions eventFlowOptions,
            Assembly fromAssembly,
            Predicate<Type> predicate = null)
        {
            predicate = predicate ?? (t => true);

            var snapshotUpgraderTypes = fromAssembly
                .GetTypes()
                .Where(t => !t.GetTypeInfo().IsAbstract)
                .Where(t => t.GetTypeInfo().GetInterfaces().Any(IsSnapshotUpgraderInterface))
                .Where(t => !t.HasConstructorParameterOfType(IsSnapshotUpgraderInterface))
                .Where(t => predicate(t));

            return eventFlowOptions.AddSnapshotUpgraders(snapshotUpgraderTypes);
        }

        public static IEventFlowOptions AddSnapshotUpgraders(
            this IEventFlowOptions eventFlowOptions,
            params Type[] snapshotUpgraderTypes)
        {
            return eventFlowOptions.AddSnapshotUpgraders((IEnumerable<Type>)snapshotUpgraderTypes);
        }

        public static IEventFlowOptions AddSnapshotUpgraders(
            this IEventFlowOptions eventFlowOptions,
            IEnumerable<Type> snapshotUpgraderTypes)
        {
            return eventFlowOptions.RegisterServices(sr =>
                {
                    foreach (var snapshotUpgraderType in snapshotUpgraderTypes)
                    {
                        var interfaceType = snapshotUpgraderType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(IsSnapshotUpgraderInterface);
                        sr.Register(interfaceType, snapshotUpgraderType);
                    }
                });
        }

        public static IEventFlowOptions UseSnapshotStore<TSnapshotStore>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique)
            where TSnapshotStore : class, ISnapshotPersistence
        {
            return eventFlowOptions.RegisterServices(sr => sr.Register<ISnapshotPersistence, TSnapshotStore>(lifetime));
        }

        public static IEventFlowOptions UseInMemorySnapshotStore(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.UseSnapshotStore<InMemorySnapshotPersistence>(Lifetime.Singleton);
        }

        private static bool IsSnapshotUpgraderInterface(Type type)
        {
            return type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(ISnapshotUpgrader<,>);
        }
    }
}