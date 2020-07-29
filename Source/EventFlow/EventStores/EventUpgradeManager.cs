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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public class EventUpgradeManager : IEventUpgradeManager
    {
        private static readonly ConcurrentDictionary<Type, EventUpgraderCacheItem> EventUpgraderCacheItems = new ConcurrentDictionary<Type, EventUpgraderCacheItem>();

        private class EventUpgraderCacheItem
        {
            public Type EventUpgraderType { get; }
            public Func<object, IDomainEvent, IEnumerable<IDomainEvent>> Upgrade { get; }

            public EventUpgraderCacheItem(Type eventUpgraderType, Func<object, IDomainEvent, IEnumerable<IDomainEvent>> upgrade)
            {
                EventUpgraderType = eventUpgraderType;
                Upgrade = upgrade;
            }
        }

        private readonly ILog _log;
        private readonly IResolver _resolver;

        public EventUpgradeManager(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public IReadOnlyCollection<IDomainEvent> Upgrade(IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            return Upgrade((IEnumerable<IDomainEvent>) domainEvents).ToList();
        }

        private IEnumerable<IDomainEvent> Upgrade(IEnumerable<IDomainEvent> domainEvents)
        {
            var domainEventList = domainEvents.ToList();
            if (!domainEventList.Any())
            {
                return Enumerable.Empty<IDomainEvent>();
            }

            var eventUpgraders = domainEventList
                .Select(d => d.AggregateType)
                .Distinct()
                .ToDictionary(
                    t => t,
                    t =>
                        {
                            var cache = GetCache(t);
                            var upgraders = _resolver.ResolveAll(cache.EventUpgraderType).OrderBy(u => u.GetType().Name).ToList();
                            return new
                                {
                                    EventUpgraders = upgraders,
                                    cache.Upgrade
                                };
                        });

            if (!eventUpgraders.Any())
            {
                return Enumerable.Empty<IDomainEvent>();
            }

            _log.Verbose(() => string.Format(
                "Upgrading {0} events and found these event upgraders to use: {1}",
                domainEventList.Count,
                string.Join(", ", eventUpgraders.Values.SelectMany(a => a.EventUpgraders.Select(e => e.GetType().PrettyPrint())))));

            return domainEventList
                .SelectMany(e =>
                    {
                        var a = eventUpgraders[e.AggregateType];
                        return a.EventUpgraders.Aggregate(
                            (IEnumerable<IDomainEvent>) new[] {e},
                            (de, up) => de.SelectMany(ee => a.Upgrade(up, ee)));
                    })
                .OrderBy(d => d.AggregateSequenceNumber);
        }

        public IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> Upgrade<TAggregate, TIdentity>(
            IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> domainEvents)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            return Upgrade(domainEvents.Cast<IDomainEvent>()).Cast<IDomainEvent<TAggregate, TIdentity>>().ToList();
        }

        private static EventUpgraderCacheItem GetCache(Type aggregateType)
        {
            return EventUpgraderCacheItems.GetOrAdd(
                aggregateType,
                t =>
                    {
                        var aggregateRootInterface = t.GetTypeInfo().GetInterfaces().SingleOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
                        if (aggregateRootInterface == null)
                        {
                            throw new ArgumentException($"Type '{t.PrettyPrint()}' is not a '{typeof(IAggregateRoot<>).PrettyPrint()}'", nameof(aggregateType));
                        }

                        var arguments = aggregateRootInterface.GetTypeInfo().GetGenericArguments();
                        var eventUpgraderType = typeof(IEventUpgrader<,>).MakeGenericType(t, arguments[0]);

                        var invokeUpgrade = ReflectionHelper.CompileMethodInvocation<Func<object, IDomainEvent, IEnumerable<IDomainEvent>>>(eventUpgraderType, "Upgrade");

                        return new EventUpgraderCacheItem(
                            eventUpgraderType,
                            invokeUpgrade);
                    });
        }
    }
}