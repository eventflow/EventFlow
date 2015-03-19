// The MIT License (MIT)
//
// Copyright (c) 2015 EventFlow
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
using System.Reflection;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow
{
    public class DispatchToEventHandlers : IDispatchToEventHandlers
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;

        private class HandlerInfomation
        {
            public Type HandlerType { get; set; }
            public MethodInfo MethodInfo { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, HandlerInfomation> HandlerInfomations = new ConcurrentDictionary<Type, HandlerInfomation>();

        public DispatchToEventHandlers(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents)
        {
            foreach (var domainEvent in domainEvents)
            {
                var handlerInfomation = GetHandlerInfomation(domainEvent.EventType);
                var handlers = _resolver.ResolveAll(handlerInfomation.HandlerType);
                foreach (var handler in handlers)
                {
                    try
                    {
                        var task = (Task)handlerInfomation.MethodInfo.Invoke(handler, new object[] { domainEvent });
                        await task.ConfigureAwait(false);
                    }
                    catch (Exception exception)
                    {
                        _log.Error(
                            exception,
                            "Failed to dispatch to event handler {0}",
                            handler.GetType().Name);
                    }
                }
            }
        }

        private static HandlerInfomation GetHandlerInfomation(Type eventType)
        {
            return HandlerInfomations.GetOrAdd(
                eventType,
                t =>
                    {
                        var handlerType = typeof(IHandleEvent<>).MakeGenericType(t);
                        return new HandlerInfomation
                            {
                                HandlerType = handlerType,
                                MethodInfo = handlerType.GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public),
                            };
                    });
        }
    }
}
