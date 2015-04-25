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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.Subscribers
{
    public class DispatchToEventSubscribers : IDispatchToEventSubscribers
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;

        private class SubscriberInfomation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        private static readonly ConcurrentDictionary<Type, SubscriberInfomation> HandlerInfomations = new ConcurrentDictionary<Type, SubscriberInfomation>();

        public DispatchToEventSubscribers(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                var subscriberInfomation = GetSubscriberInfomation(domainEvent.EventType);
                var subscribers = _resolver.ResolveAll(subscriberInfomation.SubscriberType);
                var subscriberDispatchTasks = subscribers
                    .Select(s => DispatchToSubscriberAsync(s, subscriberInfomation, domainEvent, cancellationToken))
                    .ToList();
                await Task.WhenAll(subscriberDispatchTasks).ConfigureAwait(false);
            }
        }

        private async Task DispatchToSubscriberAsync(object handler, SubscriberInfomation subscriberInfomation, IDomainEvent domainEvent, CancellationToken cancellationToken)
        {
            try
            {
                _log.Verbose(
                    "Calling HandleAsync on handler '{0}' for aggregate event '{1}'",
                    handler.GetType().Name,
                    domainEvent.EventType.Name);
                await subscriberInfomation.HandleMethod(handler, domainEvent, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                _log.Error(
                    exception,
                    "Failed to dispatch to event handler {0}",
                    handler.GetType().Name);
            }
        }

        private static SubscriberInfomation GetSubscriberInfomation(Type eventType)
        {
            return HandlerInfomations.GetOrAdd(
                eventType,
                t =>
                    {
                        var handlerType = typeof(ISubscribeSynchronousTo<>).MakeGenericType(t);
                        var methodInfo = handlerType.GetMethod("HandleAsync", BindingFlags.Instance | BindingFlags.Public);
                        return new SubscriberInfomation
                            {
                                SubscriberType = handlerType,
                                HandleMethod = (Func<object, IDomainEvent, CancellationToken, Task>) ((h, e, c) => (Task) methodInfo.Invoke(h, new object[] {e, c}))
                            };
                    });
        }
    }
}
