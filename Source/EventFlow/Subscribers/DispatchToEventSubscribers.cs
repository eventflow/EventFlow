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
using System.Linq;
using System.Reflection;
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
        private static readonly Type SubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type SubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);

        private readonly ILog _log;
        private readonly IResolver _resolver;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;
        private readonly IMemoryCache _memoryCache;
        private readonly IDispatchToSubscriberResilienceStrategy _dispatchToSubscriberResilienceStrategy;

        private class SubscriberInformation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        public DispatchToEventSubscribers(
            ILog log,
            IResolver resolver,
            IEventFlowConfiguration eventFlowConfiguration,
            IMemoryCache memoryCache,
            IDispatchToSubscriberResilienceStrategy dispatchToSubscriberResilienceStrategy)
        {
            _log = log;
            _resolver = resolver;
            _eventFlowConfiguration = eventFlowConfiguration;
            _memoryCache = memoryCache;
            _dispatchToSubscriberResilienceStrategy = dispatchToSubscriberResilienceStrategy;
        }

        public async Task DispatchToSynchronousSubscribersAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            foreach (var domainEvent in domainEvents)
            {
                await _dispatchToSubscriberResilienceStrategy.BeforeDispatchToSubscribersAsync(
                    domainEvent,
                    domainEvents,
                    cancellationToken);
                try
                {
                    await DispatchToSubscribersAsync(
                            domainEvent,
                            SubscribeSynchronousToType,
                            !_eventFlowConfiguration.ThrowSubscriberExceptions,
                            cancellationToken)
                        .ConfigureAwait(false);
                    await _dispatchToSubscriberResilienceStrategy.DispatchToSubscribersSucceededAsync(
                            domainEvent,
                            domainEvents,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    if (!await _dispatchToSubscriberResilienceStrategy.HandleDispatchToSubscribersFailedAsync(
                            domainEvent,
                            domainEvents,
                            e,
                            cancellationToken)
                        .ConfigureAwait(false))
                    {
                        throw;
                    }
                }
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
            var subscriberInformation = await GetSubscriberInformationAsync(
                    domainEvent.GetType(),
                    subscriberType,
                    cancellationToken)
                .ConfigureAwait(false);
            var subscribers = _resolver.ResolveAll(subscriberInformation.SubscriberType)
                .Cast<ISubscribe>()
                .OrderBy(s => s.GetType().Name)
                .ToList();

            if (!subscribers.Any())
            {
                _log.Debug(() => $"Didn't find any subscribers to '{domainEvent.EventType.PrettyPrint()}'");
                return;
            }

            var exceptions = new List<Exception>();
            foreach (var subscriber in subscribers)
            {
                try
                {
                    await DispatchToSubscriberAsync(
                            domainEvent,
                            subscriber,
                            subscriberInformation,
                            swallowException,
                            cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }

            if (exceptions.Any())
            {
                throw new AggregateException(
                    $"Dispatch of domain event {domainEvent.GetType().PrettyPrint()} to subscribers failed",
                    exceptions);
            }
        }

        private async Task DispatchToSubscriberAsync(
            IDomainEvent domainEvent,
            ISubscribe subscriber,
            SubscriberInformation subscriberInformation,
            bool swallowException,
            CancellationToken cancellationToken)
        {
            _log.Verbose(() => $"Calling HandleAsync on handler '{subscriber.GetType().PrettyPrint()}' " +
                               $"for aggregate event '{domainEvent.EventType.PrettyPrint()}'");

            await _dispatchToSubscriberResilienceStrategy.BeforeHandleEventAsync(
                    subscriber,
                    domainEvent,
                    cancellationToken)
                .ConfigureAwait(false);
            try
            {
                await subscriberInformation.HandleMethod(
                        subscriber,
                        domainEvent,
                        cancellationToken)
                    .ConfigureAwait(false);
                await _dispatchToSubscriberResilienceStrategy.HandleEventSucceededAsync(
                        subscriber,
                        domainEvent,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (swallowException)
            {
                _log.Error(e, $"Subscriber '{subscriberInformation.SubscriberType.PrettyPrint()}' threw " +
                              $"'{e.GetType().PrettyPrint()}' while handling '{domainEvent.EventType.PrettyPrint()}': {e.Message}");
                await _dispatchToSubscriberResilienceStrategy.HandleEventFailedAsync(
                        subscriber,
                        domainEvent,
                        e,
                        true,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e) when (!swallowException)
            {
                await _dispatchToSubscriberResilienceStrategy.HandleEventFailedAsync(
                        subscriber,
                        domainEvent,
                        e,
                        false,
                        cancellationToken)
                    .ConfigureAwait(false);
                throw;
            }
        }

        private Task<SubscriberInformation> GetSubscriberInformationAsync(
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
                            .Single(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IDomainEvent<,,>))
                            .GetTypeInfo()
                            .GetGenericArguments();

                        var handlerType = subscriberType.MakeGenericType(arguments[0], arguments[1], arguments[2]);
                        var invokeHandleAsync = ReflectionHelper.CompileMethodInvocation<Func<object, IDomainEvent, CancellationToken, Task>>(handlerType, "HandleAsync");
                        
                        return Task.FromResult(new SubscriberInformation
                            {
                                SubscriberType = handlerType,
                                HandleMethod = invokeHandleAsync,
                            });
                    },
                cancellationToken);
        }
    }
}