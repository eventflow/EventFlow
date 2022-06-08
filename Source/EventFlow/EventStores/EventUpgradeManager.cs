// The MIT License (MIT)
// 
// Copyright (c) 2015-2022 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.EventStores
{
    public class EventUpgradeManager : IEventUpgradeManager
    {
        private static readonly ConcurrentDictionary<Type, EventUpgraderCacheItem> EventUpgraderCacheItems = new ConcurrentDictionary<Type, EventUpgraderCacheItem>();

        private class EventUpgraderCacheItem
        {
            public Type EventUpgraderType { get; }
            public Func<object, IDomainEvent, IEventUpgradeContext, CancellationToken, IAsyncEnumerable<IDomainEvent>> Upgrade { get; }

            public EventUpgraderCacheItem(Type eventUpgraderType, Func<object, IDomainEvent, IEventUpgradeContext, CancellationToken, IAsyncEnumerable<IDomainEvent>> upgrade)
            {
                EventUpgraderType = eventUpgraderType;
                Upgrade = upgrade;
            }
        }

        private readonly ILogger<EventUpgradeManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EventUpgradeManager(
            ILogger<EventUpgradeManager> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        public IAsyncEnumerable<IDomainEvent> UpgradeAsync(
            IAsyncEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            return Upgrade((IEnumerable<IDomainEvent>) domainEvents);
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
                            var upgraders = _serviceProvider.GetServices(cache.EventUpgraderType)
                                .OrderBy(u => u.GetType().Name)
                                .ToList();
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

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    "Upgrading {DomainEventCount} events and found these event upgraders to use: {EventUpgraderTypes}",
                    domainEventList.Count,
                    eventUpgraders.Values.SelectMany(a => a.EventUpgraders.Select(e => e.GetType().PrettyPrint())).ToList());
            }

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

        public IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> UpgradeAsync<TAggregate, TIdentity>(
            IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> domainEvents, CancellationToken cancellationToken)
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
