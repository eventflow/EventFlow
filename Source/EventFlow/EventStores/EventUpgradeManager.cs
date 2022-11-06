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
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.EventStores
{
    public class EventUpgradeManager : IEventUpgradeManager
    {
        private readonly ILogger<EventUpgradeManager> _logger;
        private readonly IServiceProvider _serviceProvider;

        public EventUpgradeManager(
            ILogger<EventUpgradeManager> logger,
            IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }


        public async IAsyncEnumerable<IDomainEvent> UpgradeAsync(
            IAsyncEnumerable<IDomainEvent> domainEvents,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var eventUpgradeContext = new EventUpgradeContext();

            await foreach (var domainEvent in domainEvents.WithCancellation(cancellationToken))
            {
                var upgradeDomainEvents = new List<IDomainEvent> { domainEvent };
                if (!eventUpgradeContext.TryGetUpgraders(domainEvent.AggregateType, out var eventUpgraders))
                {
                    eventUpgraders = ResolveUpgraders(domainEvent.AggregateType, domainEvent.IdentityType);
                    eventUpgradeContext.AddUpgraders(domainEvent.AggregateType, eventUpgraders);
                }

                foreach (var eventUpgrader in eventUpgraders)
                {
                    var buffer = new List<IDomainEvent>();

                    foreach (var upgradeDomainEvent in upgradeDomainEvents)
                    {
                        await foreach (var upgradedDomainEvent in eventUpgrader.UpgradeAsync(upgradeDomainEvent, eventUpgradeContext, cancellationToken))
                        {
                            buffer.Add(upgradedDomainEvent);
                        }
                    }

                    upgradeDomainEvents = buffer;
                }

                foreach (var upgradeDomainEvent in upgradeDomainEvents)
                {
                    yield return upgradeDomainEvent;
                }
            }
        }

        public async IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> UpgradeAsync<TAggregate, TIdentity>(
            IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> domainEvents,
            [EnumeratorCancellation] CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var eventUpgradeContext = new EventUpgradeContext();
            var eventUpgraders = ResolveUpgraders(typeof(TAggregate), typeof(TIdentity));
            eventUpgradeContext.AddUpgraders(typeof(TAggregate), eventUpgraders);

            await foreach (var domainEvent in domainEvents.WithCancellation(cancellationToken))
            {
                var upgradeDomainEvents = new List<IDomainEvent<TAggregate, TIdentity>>{domainEvent};

                foreach (var eventUpgrader in eventUpgraders)
                {
                    var buffer = new List<IDomainEvent<TAggregate, TIdentity>>();

                    foreach (var upgradeDomainEvent in upgradeDomainEvents)
                    {
                        await foreach (var upgradedDomainEvent in eventUpgrader.UpgradeAsync(upgradeDomainEvent, eventUpgradeContext, cancellationToken))
                        {
                            buffer.Add((IDomainEvent<TAggregate, TIdentity>) upgradedDomainEvent);
                        }
                    }

                    upgradeDomainEvents = buffer;
                }

                foreach (var upgradeDomainEvent in upgradeDomainEvents)
                {
                    yield return upgradeDomainEvent;
                }
            }
        }

        private IReadOnlyCollection<IEventUpgrader> ResolveUpgraders(Type aggregateType, Type identityType)
        {
             var type = typeof(IEventUpgrader<,>).MakeGenericType(aggregateType, identityType);
             return _serviceProvider.GetServices(type)
                 .OrderBy(u => u.GetType().Name)
                 .Select(u => (IEventUpgrader)u)
                 .ToList();
        }
    }
}
