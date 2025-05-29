// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EventFlow.Subscribers
{
    public class DispatchToEventSubscribers : IDispatchToEventSubscribers
    {
        private static readonly Type SubscribeSynchronousToType = typeof(ISubscribeSynchronousTo<,,>);
        private static readonly Type SubscribeAsynchronousToType = typeof(ISubscribeAsynchronousTo<,,>);

        private readonly ILogger<DispatchToEventSubscribers> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IEventFlowConfiguration _eventFlowConfiguration;
        private readonly IMemoryCache _memoryCache;
        private readonly IDispatchToSubscriberResilienceStrategy _dispatchToSubscriberResilienceStrategy;

        private class SubscriberInformation
        {
            public Type SubscriberType { get; set; }
            public Func<object, IDomainEvent, CancellationToken, Task> HandleMethod { get; set; }
        }

        public DispatchToEventSubscribers(
            ILogger<DispatchToEventSubscribers> logger,
            IServiceProvider serviceProvider,
            IEventFlowConfiguration eventFlowConfiguration,
            IMemoryCache memoryCache,
            IDispatchToSubscriberResilienceStrategy dispatchToSubscriberResilienceStrategy)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
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
            var subscribers = _serviceProvider.GetServices(subscriberInformation.SubscriberType)
                .Cast<ISubscribe>()
                .GroupBy(s => s.GetType())
                .Select(g => g.First())
                .OrderBy(s => s.GetType().Name)
                .ToList();

            if (!subscribers.Any())
            {
                _logger.LogDebug("Didn't find any subscribers to {EventType}", domainEvent.EventType.PrettyPrint());
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
            _logger.LogTrace("Calling HandleAsync on handler {SubscriberType} for aggregate event {EventType}",
                subscriber.GetType().PrettyPrint(),
                domainEvent.EventType.PrettyPrint());

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
                _logger.LogError(
                    e, "Subscriber {SubscriberType} threw {ExceptionType} while handling {EventType}: {ExceptionMessage}",
                    subscriberInformation.SubscriberType.PrettyPrint(),
                    e.GetType().PrettyPrint(),
                    domainEvent.EventType.PrettyPrint(),
                    e.Message);

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
            CancellationToken _)
        {
            return _memoryCache.GetOrCreate(
                CacheKey.With(GetType(), domainEventType.GetCacheKey(), subscriberType.GetCacheKey()),
                e =>
                    {
                        e.AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1);
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
                    });
        }
    }
}
