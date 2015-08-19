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
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Configuration.Registrations;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Logs;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class EventFlowOptions
    {
        public static EventFlowOptions New => new EventFlowOptions();

        private readonly ConcurrentBag<Type> _aggregateEventTypes = new ConcurrentBag<Type>();
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();
        private Lazy<IServiceRegistration> _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => new AutofacServiceRegistration()); 

        private EventFlowOptions() { }

        public EventFlowOptions ConfigureOptimisticConcurrentcyRetry(int retries, TimeSpan delayBeforeRetry)
        {
            _eventFlowConfiguration.NumberOfRetriesOnOptimisticConcurrencyExceptions = retries;
            _eventFlowConfiguration.DelayBeforeRetryOnOptimisticConcurrencyExceptions = delayBeforeRetry;
            return this;
        }

        public EventFlowOptions Configure(Action<EventFlowConfiguration> configure)
        {
            configure(_eventFlowConfiguration);
            return this;
        }

        public EventFlowOptions AddEvents(IEnumerable<Type> aggregateEventTypes)
        {
            foreach (var aggregateEventType in aggregateEventTypes)
            {
                if (!typeof(IAggregateEvent).IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException($"Type {aggregateEventType.Name} is not a {typeof (IAggregateEvent).Name}");
                }
                _aggregateEventTypes.Add(aggregateEventType);
            }
            return this;
        }

        public EventFlowOptions RegisterServices(Action<IServiceRegistration> register)
        {
            register(_lazyRegistrationFactory.Value);
            return this;
        }

        public EventFlowOptions UseServiceRegistration(IServiceRegistration serviceRegistration)
        {
            if (_lazyRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException("Registration factory is already in use");
            }

            _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => serviceRegistration);
            return this;
        }

        public IRootResolver CreateResolver(bool validateRegistrations = true)
        {
            var services = new HashSet<Type>(_lazyRegistrationFactory.Value.GetRegisteredServices());

            RegisterIfMissing<ILog, ConsoleLog>(services);
            RegisterIfMissing<IEventStore, InMemoryEventStore>(services, Lifetime.Singleton);
            RegisterIfMissing<ICommandBus, CommandBus>(services);
            RegisterIfMissing<IReadModelPopulator, ReadModelPopulator>(services);
            RegisterIfMissing<IEventJsonSerializer, EventJsonSerializer>(services);
            RegisterIfMissing<IEventDefinitionService, EventDefinitionService>(services, Lifetime.Singleton);
            RegisterIfMissing<IQueryProcessor, QueryProcessor>(services, Lifetime.Singleton);
            RegisterIfMissing<IJsonSerializer, JsonSerializer>(services);
            RegisterIfMissing<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>(services);
            RegisterIfMissing<IEventUpgradeManager, EventUpgradeManager>(services, Lifetime.Singleton);
            RegisterIfMissing<IAggregateFactory, AggregateFactory>(services);
            RegisterIfMissing<IReadModelDomainEventApplier, ReadModelDomainEventApplier>(services);
            RegisterIfMissing<IDomainEventPublisher, DomainEventPublisher>(services);
            RegisterIfMissing<IDispatchToEventSubscribers, DispatchToEventSubscribers>(services);
            RegisterIfMissing<IDomainEventFactory, DomainEventFactory>(services, Lifetime.Singleton);
            RegisterIfMissing<IEventFlowConfiguration>(services, f => f.Register<IEventFlowConfiguration>(_ => _eventFlowConfiguration));

            if (!services.Contains(typeof (ITransientFaultHandler<>)))
            {
                _lazyRegistrationFactory.Value.RegisterGeneric(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            }

            var rootResolver = _lazyRegistrationFactory.Value.CreateResolver(validateRegistrations);

            var eventDefinitionService = rootResolver.Resolve<IEventDefinitionService>();
            eventDefinitionService.LoadEvents(_aggregateEventTypes);

            return rootResolver;
        }

        private void RegisterIfMissing<TService, TImplementation>(ICollection<Type> registeredServices, Lifetime lifetime = Lifetime.AlwaysUnique)
            where TService : class
            where TImplementation : class, TService
        {
            RegisterIfMissing<TService>(registeredServices, f => f.Register<TService, TImplementation>(lifetime));
        }

        private void RegisterIfMissing<TService>(ICollection<Type> registeredServices, Action<IServiceRegistration> register)
        {
            if (registeredServices.Contains(typeof (TService)))
            {
                return;
            }
            register(_lazyRegistrationFactory.Value);
        }
    }
}
