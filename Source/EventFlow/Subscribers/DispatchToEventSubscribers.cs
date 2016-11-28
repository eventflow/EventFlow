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
using System.Reflection;

namespace EventFlow.Subscribers
{
    public class DispatchToEventSubscribers : IDispatchToEventSubscribers
    {
        private static readonly Type SubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type SubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);

        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;
        private readonly IMemoryCache _memoryCache;
        private readonly ITaskRunner _taskRunner;

        private class SubscriberInfomation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        public DispatchToEventSubscribers(
            ILog log,
            IResolver resolver,
            IEventFlowConfiguration eventFlowConfiguration,
            IMemoryCache memoryCache,
            ITaskRunner taskRunner)
        {
            _log = log;
            _resolver = resolver;
            _eventFlowConfiguration = eventFlowConfiguration;
            _memoryCache = memoryCache;
            _taskRunner = taskRunner;
        }

        public async Task DispatchAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            DispatchToAsynchronousSubscribers(domainEvents, cancellationToken);
            await DispatchToSynchronousSubscribersAsync(domainEvents, cancellationToken).ConfigureAwait(false);
        }

        private void DispatchToAsynchronousSubscribers(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            _taskRunner.Run(
                Label.Named("publish-to-asynchronous-subscribers"),
                (r, c) => Task.WhenAll(domainEvents.Select(d => DispatchToSubscribersAsync(d, r, SubscribeAsynchronousToType, true, c))),
                cancellationToken);
        }

        private async Task DispatchToSynchronousSubscribersAsync(
            IEnumerable<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                await DispatchToSubscribersAsync(
                        domainEvent,
                        _resolver,
                        SubscribeSynchronousToType,
                        !_eventFlowConfiguration.ThrowSubscriberExceptions,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task DispatchToSubscribersAsync(
            IDomainEvent domainEvent,
            IResolver resolver,
            Type subscriberType,
            bool swallowException,
            CancellationToken cancellationToken)
        {
            var subscriberInfomation = await GetSubscriberInfomationAsync(
                    domainEvent.GetType(),
                    subscriberType,
                    cancellationToken)
                .ConfigureAwait(false);
            var subscribers = resolver.ResolveAll(subscriberInfomation.SubscriberType);

            foreach (var subscriber in subscribers)
            {
                _log.Verbose(() => $"Calling HandleAsync on handler '{subscriber.GetType().PrettyPrint()}' " +
                                   $"for aggregate event '{domainEvent.EventType.PrettyPrint()}'");

                try
                {
                    await subscriberInfomation.HandleMethod(subscriber, domainEvent, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) when (swallowException)
                {
                    _log.Error(e, $"Subscriber '{subscriberInfomation.SubscriberType.PrettyPrint()}' threw " +
                                  $"'{e.GetType().PrettyPrint()}' while handling '{domainEvent.EventType.PrettyPrint()}': {e.Message}");
                }
            }
        }

        private Task<SubscriberInfomation> GetSubscriberInfomationAsync(
            Type domainEventType,
            Type subscriberType,
            CancellationToken cancellationToken)
        {
            return _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), domainEventType.GetCacheKey(), subscriberType.GetCacheKey()),
                TimeSpan.FromDays(1), 
                _ =>
                    {
                        var arguments = domainEventType
                            .GetTypeInfo()
                            .GetInterfaces()
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof (IDomainEvent<,,>))
                            .GetTypeInfo()
                            .GetGenericArguments();

                        var handlerType = subscriberType.MakeGenericType(arguments[0], arguments[1], arguments[2]);
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