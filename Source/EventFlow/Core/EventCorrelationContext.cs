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
using System.Threading;

namespace EventFlow.Core
{
    public enum EventAction
    {
        AggregateStore
    }

    public static class EventCorrelationContext
    {
        private static readonly AsyncLocal<IEventContext> Current = new AsyncLocal<IEventContext>();

        public static IEventContext Peek => Current.Value;

        public static IDisposable Push(EventAction eventAction)
        {
            return Push(p => new EventFlowContext(eventAction, p));
        }

        internal static IDisposable Push(Func<IEventContext, IEventContext> factory)
        {
            var previous = Current.Value;
            var current = factory(previous);

            if (ReferenceEquals(current, previous))
            {
                throw new InvalidOperationException("You must supply a new instance");
            }

            Current.Value = current;

            return new DisposableAction(() =>
                {
                    Current.Value = previous;
                });
        }

        private class EventFlowContext : IEventContext
        {
            public Guid Id { get; } = Guid.NewGuid();
            public EventAction EventAction { get; }
            public IEventContext Previous { get; }

            public EventFlowContext(
                EventAction eventAction,
                IEventContext previous)
            {
                if (!Enum.IsDefined(typeof(EventAction), eventAction))
                {
                    throw new ArgumentOutOfRangeException(
                        nameof(eventAction),
                        eventAction, $"Unknown '{nameof(EventAction)}' value");
                }

                EventAction = eventAction;
                Previous = previous;
            }
        }
    }
}
