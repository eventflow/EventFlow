// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.Subscribers
{
    public class DispatchToEventSubscribers : IDispatchToEventSubscribers
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;
        private readonly IMemoryCache _memoryCache;

        private class SubscriberInfomation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        public DispatchToEventSubscribers(
            ILog log,
            IResolver resolver,
            IEventFlowConfiguration eventFlowConfiguration,
            IMemoryCache memoryCache)
        {
            _log = log;
            _resolver = resolver;
            _eventFlowConfiguration = eventFlowConfiguration;
            _memoryCache = memoryCache;
        }

        public async Task DispatchAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                var subscriberInfomation = await GetSubscriberInfomationAsync(domainEvent.GetType(), cancellationToken).ConfigureAwait(false);
                var subscribers = _resolver.ResolveAll(subscriberInfomation.SubscriberType);
                var subscriberDispatchTasks = subscribers
                    .Select(s => DispatchToSubscriberAsync(s, subscriberInfomation, domainEvent, cancellationToken))
                    .ToList();

                try
                {
                    await Task.WhenAll(subscriberDispatchTasks).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (_eventFlowConfiguration.ThrowSubscriberExceptions)
                    {
                        throw;
                    }

                    _log.Error(
                        exception,
                        $"Event subscribers of event '{domainEvent.EventType.PrettyPrint()}' threw an unexpected exception!");
                }
            }
        }

        private async Task DispatchToSubscriberAsync(
            object handler,
            SubscriberInfomation subscriberInfomation,
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            _log.Verbose(() => string.Format(
                "Calling HandleAsync on handler '{0}' for aggregate event '{1}'",
                handler.GetType().PrettyPrint(),
                domainEvent.EventType.PrettyPrint()));

            await subscriberInfomation.HandleMethod(handler, domainEvent, cancellationToken).ConfigureAwait(false);
        }

        private Task<SubscriberInfomation> GetSubscriberInfomationAsync(Type domainEventType, CancellationToken cancellationToken)
        {
            return _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), domainEventType.GetCacheKey()),
                TimeSpan.FromDays(1), 
                _ =>
                    {
                        var arguments = domainEventType
                            .GetInterfaces()
                            .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IDomainEvent<,,>))
                            .GetGenericArguments();

                        var handlerType = typeof(ISubscribeSynchronousTo<,,>).MakeGenericType(arguments[0], arguments[1], arguments[2]);
                        var invokeHandleAsync = ReflectionHelper.CompileMethodInvocation<Func<object, IDomainEvent, CancellationToken, Task>>(handlerType, "HandleAsync");

                        return Task.FromResult(new SubscriberInfomation
                            {
                                SubscriberType = handlerType,
                                HandleMethod = invokeHandleAsync,
                            });
                    },
                cancellationToken);
        }
    }
}
