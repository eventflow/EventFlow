// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.TestHelpers.Aggregates.Events;

namespace EventFlow.TestHelpers.Aggregates.Upgraders
{
    public class ThingyUpgradableV1ToV2EventUpgrader : EventUpgraderNonAsync<ThingyAggregate, ThingyId>
    {
        private readonly IDomainEventFactory _domainEventFactory;

        public ThingyUpgradableV1ToV2EventUpgrader(
            IDomainEventFactory domainEventFactory)
        {
            _domainEventFactory = domainEventFactory;
        }

        protected override IEnumerable<IDomainEvent<ThingyAggregate, ThingyId>> Upgrade(
            IDomainEvent<ThingyAggregate, ThingyId> domainEvent)
        {
            if (!(domainEvent is IDomainEvent<ThingyAggregate, ThingyId, ThingyUpgradableV1Event> _))
            {
                yield return domainEvent;
            }
            else
            {
                yield return _domainEventFactory.Upgrade<ThingyAggregate, ThingyId>(
                    domainEvent,
                    new ThingyUpgradableV2Event());
            }
        }
    }
}
