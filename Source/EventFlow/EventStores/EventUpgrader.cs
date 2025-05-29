// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Core;

#pragma warning disable CS1998

namespace EventFlow.EventStores
{
    public abstract class EventUpgraderNonAsync<TAggregate, TIdentity> : EventUpgrader<TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        protected abstract IEnumerable<IDomainEvent<TAggregate, TIdentity>> Upgrade(
            IDomainEvent<TAggregate, TIdentity> domainEvent);

        public override async IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> UpgradeAsync(
            IDomainEvent<TAggregate, TIdentity> domainEvent,
            IEventUpgradeContext eventUpgradeContext,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            // We check as it now before calling as it is not passed to the legacy method
            cancellationToken.ThrowIfCancellationRequested();

            foreach (var upgradedDomainEvent in Upgrade(domainEvent))
            {
                yield return upgradedDomainEvent;
            }
        }
    }

    public abstract class EventUpgrader<TAggregate, TIdentity> : IEventUpgrader<TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        public virtual async IAsyncEnumerable<IDomainEvent> UpgradeAsync(
            IDomainEvent domainEvent,
            IEventUpgradeContext eventUpgradeContext,
            [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var castDomainEvent = (IDomainEvent<TAggregate, TIdentity>) domainEvent;
            await foreach (var e in UpgradeAsync(castDomainEvent, eventUpgradeContext, cancellationToken))
            {
                yield return e;
            }
        }

        public abstract IAsyncEnumerable<IDomainEvent<TAggregate, TIdentity>> UpgradeAsync(
            IDomainEvent<TAggregate, TIdentity> domainEvent,
            IEventUpgradeContext eventUpgradeContext,
            CancellationToken cancellationToken);
    }
}
