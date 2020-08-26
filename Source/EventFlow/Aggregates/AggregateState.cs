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
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.Aggregates
{
    public abstract class AggregateState<TAggregate, TIdentity, TEventApplier> : IEventApplier<TAggregate, TIdentity>
        where TEventApplier : class, IEventApplier<TAggregate, TIdentity>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private static readonly IReadOnlyDictionary<Type, Action<TEventApplier, IAggregateEvent>> ApplyMethods; 

        static AggregateState()
        {
            ApplyMethods = typeof(TEventApplier).GetAggregateEventApplyMethods<TAggregate, TIdentity, TEventApplier>();
        }

        protected AggregateState()
        {
            var me = this as TEventApplier;
            if (me == null)
            {
                throw new InvalidOperationException(
                    $"Event applier of type '{GetType().PrettyPrint()}' has a wrong generic argument '{typeof(TEventApplier).PrettyPrint()}'");
            }
        }

        public bool Apply(
            TAggregate aggregate,
            IAggregateEvent<TAggregate, TIdentity> aggregateEvent)
        {
            var aggregateEventType = aggregateEvent.GetType();
            Action<TEventApplier, IAggregateEvent> applier;

            if (!ApplyMethods.TryGetValue(aggregateEventType, out applier))
            {
                return false;
            }

            applier((TEventApplier) (object) this, aggregateEvent);
            return true;
        }
    }
}