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
using System.Collections.Concurrent;
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
        private readonly ConcurrentDictionary<Type, IReadOnlyCollection<IEventUpgrader>> _eventUpgraders = new ConcurrentDictionary<Type, IReadOnlyCollection<IEventUpgrader>>(); 

        public EventUpgradeManager(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public IReadOnlyCollection<IDomainEvent> Upgrade<TAggregate>(IReadOnlyCollection<IDomainEvent> domainEvents)
            where TAggregate : IAggregateRoot
        {
            var aggreateType = typeof (TAggregate);
            var eventUpgraders = GetEventUpgraders<TAggregate>();

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
                .Select(e => eventUpgraders.Aggregate(e, (de, up) => up.Upgrade(de)))
                .ToList();
        }

        private IReadOnlyCollection<IEventUpgrader> GetEventUpgraders<TAggregate>()
            where TAggregate : IAggregateRoot
        {
            return _eventUpgraders.GetOrAdd(
                typeof (TAggregate),
                t => _resolver
                    .Resolve<IEnumerable<IEventUpgrader<TAggregate>>>()
                    .OrderBy(u => u.GetType().Name)
                    .ToList());
        }
    }
}
