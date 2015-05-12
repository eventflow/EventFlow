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
using System.Collections.Generic;
using System.Linq;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.EventStores
{
    public class EventUpgradeManager : IEventUpgradeManager
    {
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
            // TODO: Clean this up!

            var eventUpgraders = domainEvents
                .Select(d => d.AggregateType)
                .Distinct()
                .ToDictionary(
                    t => t,
                    t =>
                        {
                            var aggregateRootInterface = t.GetInterfaces().Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));
                            var arguments = aggregateRootInterface.GetGenericArguments();
                            var eventUpgraderType = typeof(IEventUpgrader<,>).MakeGenericType(t, arguments[0]);
                            var methodInfo = eventUpgraderType.GetMethod("Upgrade");
                            return new
                                {
                                    EventUpgraders = _resolver.ResolveAll(eventUpgraderType).OrderBy(u => u.GetType().Name).ToList(),
                                    Invoker = (Func<object, IDomainEvent, IEnumerable<IDomainEvent>>)((o, e) => ((IEnumerable) methodInfo.Invoke(o, new object[]{e})).Cast<IDomainEvent>())
                                };
                        });

            return domainEvents
                .SelectMany(e =>
                    {
                        var a = eventUpgraders[e.AggregateType];
                        return a.EventUpgraders.Aggregate(
                            (IEnumerable<IDomainEvent>) new[] {e},
                            (de, up) => de.SelectMany(ee => a.Invoker(up, ee)));
                    })
                .OrderBy(d => d.GlobalSequenceNumber)
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
                .OrderBy(e => e.GlobalSequenceNumber)
                .ToList();
        }
    }
}
