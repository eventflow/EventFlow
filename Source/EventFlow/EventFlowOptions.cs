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
using System.Collections.Generic;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.Bootstraps;
using EventFlow.Configuration.Registrations;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using EventFlow.Jobs;
using EventFlow.Logs;
using EventFlow.Provided;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class EventFlowOptions : IEventFlowOptions
    {
        private readonly List<Type> _aggregateEventTypes = new List<Type>();
        private readonly List<Type> _commandTypes = new List<Type>();
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();
        private readonly List<Type> _jobTypes = new List<Type>();
        private Lazy<IModuleRegistration> _lazyModuleRegistrationFactory;
        private Lazy<IServiceRegistration> _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => new AutofacServiceRegistration());
        private bool _isInitialized = false;

        private EventFlowOptions()
        {
            _lazyModuleRegistrationFactory = new Lazy<IModuleRegistration>(() => new ModuleRegistration(this));
        }

        public static EventFlowOptions New => new EventFlowOptions();

        public IModuleRegistration ModuleRegistration => _lazyModuleRegistrationFactory.Value;

        public IEventFlowOptions ConfigureOptimisticConcurrentcyRetry(int retries, TimeSpan delayBeforeRetry)
        {
            AssertNotInitialized();

            _eventFlowConfiguration.NumberOfRetriesOnOptimisticConcurrencyExceptions = retries;
            _eventFlowConfiguration.DelayBeforeRetryOnOptimisticConcurrencyExceptions = delayBeforeRetry;
            return this;
        }

        public IEventFlowOptions Configure(Action<EventFlowConfiguration> configure)
        {
            AssertNotInitialized();

            configure(_eventFlowConfiguration);
            return this;
        }

        public IEventFlowOptions AddEvents(IEnumerable<Type> aggregateEventTypes)
        {
            AssertNotInitialized();

            foreach (var aggregateEventType in aggregateEventTypes)
            {
                if (!typeof (IAggregateEvent).IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException($"Type {aggregateEventType.Name} is not a {typeof (IAggregateEvent).Name}");
                }
                _aggregateEventTypes.Add(aggregateEventType);
            }
            return this;
        }

        public IEventFlowOptions AddCommands(IEnumerable<Type> commandTypes)
        {
            AssertNotInitialized();

            foreach (var commandType in commandTypes)
            {
                if (!typeof (ICommand).IsAssignableFrom(commandType))
                {
                    throw new ArgumentException($"Type {commandType.Name} is not a {typeof (ICommand).PrettyPrint()}");
                }
                _commandTypes.Add(commandType);
            }
            return this;
        }

        public IEventFlowOptions AddJobs(IEnumerable<Type> jobTypes)
        {
            AssertNotInitialized();

            foreach (var jobType in jobTypes)
            {
                if (!typeof (IJob).IsAssignableFrom(jobType))
                {
                    throw new ArgumentException($"Type {jobType.Name} is not a {typeof (IJob).PrettyPrint()}");
                }
                _jobTypes.Add(jobType);
            }
            return this;
        }

        public IEventFlowOptions RegisterServices(Action<IServiceRegistration> register)
        {
            AssertNotInitialized();

            register(_lazyRegistrationFactory.Value);
            return this;
        }

        public IEventFlowOptions RegisterModule<TModule>()
            where TModule : IModule, new()
        {
            AssertNotInitialized();

            _lazyModuleRegistrationFactory.Value.Register<TModule>();
            return this;
        }

        public IEventFlowOptions RegisterModule<TModule>(TModule module)
            where TModule : IModule
        {
            AssertNotInitialized();

            _lazyModuleRegistrationFactory.Value.Register(module);
            return this;
        }

        public IEventFlowOptions UseServiceRegistration(IServiceRegistration serviceRegistration)
        {
            AssertNotInitialized();

            if (_lazyRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException("Service registration is already in use");
            }

            _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => serviceRegistration);
            return this;
        }

        public IEventFlowOptions UseModuleRegistration(IModuleRegistration moduleRegistration)
        {
            AssertNotInitialized();

            if (_lazyModuleRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException($"Module registration is already in use");
            }
            _lazyModuleRegistrationFactory = new Lazy<IModuleRegistration>(() => moduleRegistration);
            return this;
        }

        public IEventFlowOptions Initialize()
        {
            AssertNotInitialized();

            var serviceRegistration = _lazyRegistrationFactory.Value;
            var moduleRegistration = _lazyModuleRegistrationFactory.Value;
            var services = new HashSet<Type>(serviceRegistration.GetRegisteredServices());

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
                serviceRegistration.RegisterGeneric(typeof (ITransientFaultHandler<>), typeof (TransientFaultHandler<>));
            }

            serviceRegistration.Register<IBootstrap, DefinitionServicesInitilizer>();
            serviceRegistration.Register(r => moduleRegistration, Lifetime.Singleton);
            moduleRegistration.Register<ProvidedJobsModule>();

            var loadedVersionedTypes = new LoadedVersionedTypes(
                _jobTypes,
                _commandTypes,
                _aggregateEventTypes);

            serviceRegistration.Register<ILoadedVersionedTypes>(r => loadedVersionedTypes, Lifetime.Singleton);

            return this;
        }

        public IRootResolver CreateResolver(bool validateRegistrations = true)
        {
            if (!_isInitialized)
            {
                Initialize();
            }

            return _lazyRegistrationFactory.Value.CreateResolver(validateRegistrations);
        }

        private void AssertNotInitialized()
        {
            if (_isInitialized)
            {
                throw new InvalidOperationException("EventFlow options have already been initialized");
            }
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