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
using System.Diagnostics;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.Registrations;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Jobs;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.Provided;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class EventFlowOptions : IEventFlowOptions
    {
        public static EventFlowOptions New => new EventFlowOptions();

        private readonly ConcurrentBag<Type> _aggregateEventTypes = new ConcurrentBag<Type>();
        private readonly ConcurrentBag<Type> _jobTypes = new ConcurrentBag<Type>(); 
        private readonly ConcurrentBag<Type> _commandTypes = new ConcurrentBag<Type>(); 
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();
        private Lazy<IServiceRegistration> _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => new AutofacServiceRegistration());
        private Lazy<IModuleRegistration> _lazyModuleRegistrationFactory; 
        private Stopwatch _stopwatch;

        private EventFlowOptions()
        {
            _stopwatch = Stopwatch.StartNew();
            _lazyModuleRegistrationFactory = new Lazy<IModuleRegistration>(() => new ModuleRegistration(this));
        }

        public IEventFlowOptions ConfigureOptimisticConcurrentcyRetry(int retries, TimeSpan delayBeforeRetry)
        {
            _eventFlowConfiguration.NumberOfRetriesOnOptimisticConcurrencyExceptions = retries;
            _eventFlowConfiguration.DelayBeforeRetryOnOptimisticConcurrencyExceptions = delayBeforeRetry;
            return this;
        }

        public IEventFlowOptions Configure(Action<EventFlowConfiguration> configure)
        {
            configure(_eventFlowConfiguration);
            return this;
        }

        public IEventFlowOptions AddEvents(IEnumerable<Type> aggregateEventTypes)
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

        public IEventFlowOptions AddCommands(IEnumerable<Type> commandTypes)
        {
            foreach (var commandType in commandTypes)
            {
                if (!typeof(ICommand).IsAssignableFrom(commandType))
                {
                    throw new ArgumentException($"Type {commandType.Name} is not a {typeof(ICommand).PrettyPrint()}");
                }
                _commandTypes.Add(commandType);
            }
            return this;
        }

        public IEventFlowOptions AddJobs(IEnumerable<Type> jobTypes)
        {
            foreach (var jobType in jobTypes)
            {
                if (!typeof(IJob).IsAssignableFrom(jobType))
                {
                    throw new ArgumentException($"Type {jobType.Name} is not a {typeof(IJob).PrettyPrint()}");
                }
                _jobTypes.Add(jobType);
            }
            return this;
        }

        public IEventFlowOptions RegisterServices(Action<IServiceRegistration> register)
        {
            register(_lazyRegistrationFactory.Value);
            return this;
        }

        public IEventFlowOptions RegisterModule<TModule>()
            where TModule : IConfigurationModule, new()
        {
            _lazyModuleRegistrationFactory.Value.Register<TModule>();
            return this;
        }

        public IEventFlowOptions RegisterModule<TModule>(TModule configurationModule)
            where TModule : IConfigurationModule
        {
            _lazyModuleRegistrationFactory.Value.Register(configurationModule);
            return this;
        }

        public IEventFlowOptions UseServiceRegistration(IServiceRegistration serviceRegistration)
        {
            if (_lazyRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException("Service registration is already in use");
            }

            _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => serviceRegistration);
            return this;
        }

        public IEventFlowOptions UseModuleRegistration(IModuleRegistration moduleRegistration)
        {
            if (_lazyModuleRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException($"Module registration is already in use");
            }
            _lazyModuleRegistrationFactory = new Lazy<IModuleRegistration>(() => moduleRegistration);
            return this;
        }

        public IRootResolver CreateResolver(bool validateRegistrations = true)
        {
            var services = new HashSet<Type>(_lazyRegistrationFactory.Value.GetRegisteredServices());

            // Add default implementations
            RegisterIfMissing<ILog, ConsoleLog>(services);
            RegisterIfMissing<IEventStore, InMemoryEventStore>(services, Lifetime.Singleton);
            RegisterIfMissing<ICommandBus, CommandBus>(services);
            RegisterIfMissing<IReadModelPopulator, ReadModelPopulator>(services);
            RegisterIfMissing<IEventJsonSerializer, EventJsonSerializer>(services);
            RegisterIfMissing<IEventDefinitionService, EventDefinitionService>(services, Lifetime.Singleton);
            RegisterIfMissing<IQueryProcessor, QueryProcessor>(services, Lifetime.Singleton);
            RegisterIfMissing<IJsonSerializer, JsonSerializer>(services);
            RegisterIfMissing<IJobScheduler, InstantJobScheduler>(services);
            RegisterIfMissing<IJobRunner, JobRunner>(services);
            RegisterIfMissing<IJobDefinitionService, JobDefinitionService>(services, Lifetime.Singleton);
            RegisterIfMissing<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>(services);
            RegisterIfMissing<IEventUpgradeManager, EventUpgradeManager>(services, Lifetime.Singleton);
            RegisterIfMissing<IAggregateFactory, AggregateFactory>(services);
            RegisterIfMissing<IReadModelDomainEventApplier, ReadModelDomainEventApplier>(services);
            RegisterIfMissing<IDomainEventPublisher, DomainEventPublisher>(services);
            RegisterIfMissing<ISerializedCommandPublisher, SerializedCommandPublisher>(services);
            RegisterIfMissing<ICommandDefinitionService, CommandDefinitionService>(services, Lifetime.Singleton);
            RegisterIfMissing<IDispatchToEventSubscribers, DispatchToEventSubscribers>(services);
            RegisterIfMissing<IDomainEventFactory, DomainEventFactory>(services, Lifetime.Singleton);
            RegisterIfMissing<IEventFlowConfiguration>(services, f => f.Register<IEventFlowConfiguration>(_ => _eventFlowConfiguration));

            if (!services.Contains(typeof (ITransientFaultHandler<>)))
            {
                _lazyRegistrationFactory.Value.RegisterGeneric(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            }

            // Add registration services
            var moduleRegistration = _lazyModuleRegistrationFactory.Value;
            _lazyRegistrationFactory.Value.Register(r => moduleRegistration, Lifetime.Singleton);

            // Provided modules
            moduleRegistration.Register<ProvidedJobsConfigurationModule>();

            // Create resolver
            var rootResolver = _lazyRegistrationFactory.Value.CreateResolver(validateRegistrations);

            // Load added type definitions into services
            var jobDefinitionService = rootResolver.Resolve<IJobDefinitionService>();
            jobDefinitionService.LoadJobs(_jobTypes);

            var eventDefinitionService = rootResolver.Resolve<IEventDefinitionService>();
            eventDefinitionService.LoadEvents(_aggregateEventTypes);

            var commandsDefinitionService = rootResolver.Resolve<ICommandDefinitionService>();
            commandsDefinitionService.LoadCommands(_commandTypes);

            // Log time spent
            _stopwatch.Stop();
            var log = rootResolver.Resolve<ILog>();
            log.Debug("EventFlow configuration done in {0:0.000} seconds", _stopwatch.Elapsed.TotalSeconds);

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