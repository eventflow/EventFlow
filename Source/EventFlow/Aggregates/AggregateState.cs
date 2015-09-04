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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using EventFlow.Core;
using EventFlow.EventSourcing;
using EventFlow.Extensions;

namespace EventFlow.Aggregates
{
    public abstract class AggregateState<TAggregate, TIdentity, TEventApplier> : IEventApplier<TAggregate, TIdentity>
        where TEventApplier : class, IEventApplier<TAggregate, TIdentity>
        where TAggregate : IEventSourced<TIdentity>
        where TIdentity : IIdentity
    {
        private static readonly Dictionary<Type, Action<TEventApplier, IEvent<TAggregate, TIdentity>>> ApplyMethods; 

        static AggregateState()
        {
            var aggregateEventType = typeof (IEvent<TAggregate, TIdentity>);

            ApplyMethods = typeof (TEventApplier)
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                .Where(mi =>
                    {
                        if (mi.Name != "Apply") return false;
                        var parameters = mi.GetParameters();
                        return
                            parameters.Length == 1 &&
                            aggregateEventType.IsAssignableFrom(parameters[0].ParameterType);
                    })
                .ToDictionary(
                    mi => mi.GetParameters()[0].ParameterType,
                    mi => (Action<TEventApplier, IEvent<TAggregate, TIdentity>>) ((ea, e) => mi.Invoke(ea, new []{ e } )));
        }

        protected AggregateState()
        {
            var me = this as TEventApplier;
            if (me == null)
            {
                throw new InvalidOperationException(
                    $"Event applier of type '{GetType().PrettyPrint()}' has a wrong generic argument '{typeof (TEventApplier).PrettyPrint()}'");
            }
        }

        public bool Apply(
            TAggregate aggregate,
            IEvent<TAggregate, TIdentity> @event)
        {
            var aggregateEventType = @event.GetType();
            Action<TEventApplier, IEvent<TAggregate, TIdentity>> applier;

            if (!ApplyMethods.TryGetValue(aggregateEventType, out applier))
            {
                return false;
            }

            applier((TEventApplier) (object) this, @event);
            return true;
        }
    }
}
