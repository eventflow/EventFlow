// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Logs;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace EventFlow.Subscribers
{
    public class DispatchToEventSubscribers : IDispatchToEventSubscribers
    {
        private static readonly Type SubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type SubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);

        private readonly ILog _log;
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<EventFlowOptions> _options;
        private readonly IMemoryCache _memoryCache;

        private class SubscriberInformation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        public DispatchToEventSubscribers(
            ILog log,
            IServiceProvider serviceProvider,
            IOptions<EventFlowOptions> options,
            IMemoryCache memoryCache)
        {
            _log = log;
            _serviceProvider = serviceProvider;
            _options = options;
            _memoryCache = memoryCache;
        }

        public async Task DispatchToSynchronousSubscribersAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                await DispatchToSubscribersAsync(
                        domainEvent,
                        SubscribeSynchronousToType,
                        !_options.Value.ThrowSubscriberExceptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public Task DispatchToAsynchronousSubscribersAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            return DispatchToSubscribersAsync(domainEvent, SubscribeAsynchronousToType, true, cancellationToken);
        }

        private async Task DispatchToSubscribersAsync(
            IDomainEvent domainEvent,
            Type subscriberType,
            bool swallowException,
            CancellationToken cancellationToken)
        {
            var subscriberInformation = GetSubscriberInformation(
                    domainEvent.GetType(),
                    subscriberType);
            var subscribers = _serviceProvider.GetServices(subscriberInformation.SubscriberType).ToList();

            if (!subscribers.Any())
            {
                _log.Debug(() => $"Didn't find any subscribers to '{domainEvent.EventType.PrettyPrint()}'");
                return;
            }

            foreach (var subscriber in subscribers)
            {
                _log.Verbose(() => $"Calling HandleAsync on handler '{subscriber.GetType().PrettyPrint()}' " +
                                   $"for aggregate event '{domainEvent.EventType.PrettyPrint()}'");

                try
                {
                    await subscriberInformation.HandleMethod(subscriber, domainEvent, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (swallowException)
                {
                    _log.Error(e, $"Subscriber '{subscriberInformation.SubscriberType.PrettyPrint()}' threw " +
                                  $"'{e.GetType().PrettyPrint()}' while handling '{domainEvent.EventType.PrettyPrint()}': {e.Message}");
                }
            }
        }

        private SubscriberInformation GetSubscriberInformation(
            Type domainEventType,
            Type subscriberType)
        {
            return _memoryCache.GetOrCreate(
                null, //TODO: CacheKey.With(GetType(), domainEventType.GetCacheKey(), subscriberType.GetCacheKey()),
                e =>
                    {
                        e.SlidingExpiration = TimeSpan.FromDays(1);
                        var arguments = domainEventType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEvent<,,>))
                            .GetTypeInfo()
                            .GetGenericArguments();

                        var handlerType = subscriberType.MakeGenericType(arguments[0], arguments[1], arguments[2]);
                        var invokeHandleAsync = ReflectionHelper.CompileMethodInvocation<Func<object, IDomainEvent, CancellationToken, Task>>(handlerType, "HandleAsync");
                        
                        return new SubscriberInformation
                            {
                                SubscriberType = handlerType,
                                HandleMethod = invokeHandleAsync,
                            };
                    });
        }
    }
}