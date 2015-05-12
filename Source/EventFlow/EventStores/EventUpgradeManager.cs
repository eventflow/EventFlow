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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public class EventUpgradeManager : IEventUpgradeManager
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;

        private static readonly MethodInfo UpgradeGeneric = typeof (EventUpgradeManager).GetMethods()
            .Single(mi => mi.Name == "Upgrade" && mi.IsGenericMethod);
        private static readonly ConcurrentDictionary<Type, Func<EventUpgradeManager, IReadOnlyCollection<IDomainEvent>, IEnumerable<IDomainEvent>>> UpgradeMethods = new ConcurrentDictionary<Type, Func<EventUpgradeManager, IReadOnlyCollection<IDomainEvent>, IEnumerable<IDomainEvent>>>(); 

        public EventUpgradeManager(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        private static Func<EventUpgradeManager, IReadOnlyCollection<IDomainEvent>, IEnumerable<IDomainEvent>> GetUpgrader(Type aggregateType, Type identityType)
        {
            return UpgradeMethods.GetOrAdd(
                aggregateType,
                _ =>
                    {
                        var methodInfo = UpgradeGeneric.MakeGenericMethod(aggregateType, identityType);
                        return (eu, de) => ((IEnumerable) (methodInfo.Invoke(eu, new object[]{ de }))).Cast<IDomainEvent>();
                    });
        }

        public IReadOnlyCollection<IDomainEvent> Upgrade(IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            return domainEvents
                .GroupBy(de => new { de.AggregateType, IdentityType = de.GetIdentity().GetType() })
                .SelectMany(g =>
                    {
                        var upgrader = GetUpgrader(g.Key.AggregateType, g.Key.IdentityType);
                        return upgrader(this, g.ToList());
                    })
                .OrderBy(de => de.GlobalSequenceNumber)
                .ToList();
        }

        public IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> Upgrade<TAggregate, TIdentity>(
            IReadOnlyCollection<IDomainEvent<TAggregate, TIdentity>> domainEvents)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            if (!domainEvents.Any())
            {
                return new IDomainEvent<TAggregate, TIdentity>[]{};
            }

            var aggreateType = typeof (TAggregate);
            var eventUpgraders = _resolver
                .Resolve<IEnumerable<IEventUpgrader<TAggregate, TIdentity>>>()
                .OrderBy(u => u.GetType().Name)
                .ToList();

            if (!eventUpgraders.Any())
            {
                _log.Verbose("No event upgraders for aggregate '{0}'", aggreateType.Name);
                return domainEvents;
            }
            
            _log.Verbose(() => string.Format(
                "Found '{0}' event upgraders for aggregate '{1}'",
                string.Join(", ", eventUpgraders.Select(u => u.GetType().Name)),
                aggreateType.Name));

            return domainEvents
                .SelectMany(e => eventUpgraders
                    .Aggregate(
                        (IEnumerable<IDomainEvent<TAggregate, TIdentity>>)new[] { e },
                        (de, up) => de.SelectMany(up.Upgrade)))
                .ToList();
        }
    }
}
