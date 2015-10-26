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
        private Lazy<IServiceRegistration> _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => new AutofacServiceRegistration());

        private EventFlowOptions()
        {
            UseServiceRegistration(new AutofacServiceRegistration());

            ModuleRegistration = new ModuleRegistration(this);
            ModuleRegistration.Register<ProvidedJobsModule>();
        }

        public static EventFlowOptions New => new EventFlowOptions();

        public IModuleRegistration ModuleRegistration { get; }

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
                if (!typeof (IAggregateEvent).IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException($"Type {aggregateEventType.PrettyPrint()} is not a {typeof (IAggregateEvent).PrettyPrint()}");
                }
                _aggregateEventTypes.Add(aggregateEventType);
            }
            return this;
        }

        public IEventFlowOptions AddCommands(IEnumerable<Type> commandTypes)
        {
            foreach (var commandType in commandTypes)
            {
                if (!typeof (ICommand).IsAssignableFrom(commandType))
                {
                    throw new ArgumentException($"Type {commandType.PrettyPrint()} is not a {typeof (ICommand).PrettyPrint()}");
                }
                _commandTypes.Add(commandType);
            }
            return this;
        }

        public IEventFlowOptions AddJobs(IEnumerable<Type> jobTypes)
        {
            foreach (var jobType in jobTypes)
            {
                if (!typeof (IJob).IsAssignableFrom(jobType))
                {
                    throw new ArgumentException($"Type {jobType.PrettyPrint()} is not a {typeof (IJob).PrettyPrint()}");
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
            where TModule : IModule, new()
        {
            ModuleRegistration.Register<TModule>();
            return this;
        }

        public IEventFlowOptions RegisterModule<TModule>(TModule module)
            where TModule : IModule
        {
            ModuleRegistration.Register(module);
            return this;
        }

        public IEventFlowOptions UseServiceRegistration(IServiceRegistration serviceRegistration)
        {
            if (_lazyRegistrationFactory != null && _lazyRegistrationFactory.IsValueCreated)
            {
                throw new InvalidOperationException("Service registration is already in use");
            }

            RegisterDefaults(serviceRegistration);
            _lazyRegistrationFactory = new Lazy<IServiceRegistration>(() => serviceRegistration);

            return this;
        }

        private void RegisterDefaults(IServiceRegistration serviceRegistration)
        {
            // http://docs.autofac.org/en/latest/register/registration.html
            // Maybe swap around and do after and and .PreserveExistingDefaults()

            serviceRegistration.Register<ILog, ConsoleLog>();
            serviceRegistration.Register<IEventStore, EventStoreBase>();
            serviceRegistration.Register<IEventPersistence, InMemoryEventPersistence>(Lifetime.Singleton);
            serviceRegistration.Register<ICommandBus, CommandBus>();
            serviceRegistration.Register<IReadModelPopulator, ReadModelPopulator>();
            serviceRegistration.Register<IEventJsonSerializer, EventJsonSerializer>();
            serviceRegistration.Register<IEventDefinitionService, EventDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<IQueryProcessor, QueryProcessor>(Lifetime.Singleton);
            serviceRegistration.Register<IJsonSerializer, JsonSerializer>();
            serviceRegistration.Register<IJobScheduler, InstantJobScheduler>();
            serviceRegistration.Register<IJobRunner, JobRunner>();
            serviceRegistration.Register<IJobDefinitionService, JobDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>();
            serviceRegistration.Register<IEventUpgradeManager, EventUpgradeManager>(Lifetime.Singleton);
            serviceRegistration.Register<IAggregateFactory, AggregateFactory>();
            serviceRegistration.Register<IReadModelDomainEventApplier, ReadModelDomainEventApplier>();
            serviceRegistration.Register<IDomainEventPublisher, DomainEventPublisher>();
            serviceRegistration.Register<ISerializedCommandPublisher, SerializedCommandPublisher>();
            serviceRegistration.Register<ICommandDefinitionService, CommandDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<IDispatchToEventSubscribers, DispatchToEventSubscribers>();
            serviceRegistration.Register<IDomainEventFactory, DomainEventFactory>(Lifetime.Singleton);
            serviceRegistration.Register<IEventFlowConfiguration>(_ => _eventFlowConfiguration);
            serviceRegistration.RegisterGeneric(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            serviceRegistration.Register<IBootstrap, DefinitionServicesInitilizer>();
            serviceRegistration.Register(_ => ModuleRegistration, Lifetime.Singleton);
            serviceRegistration.Register<ILoadedVersionedTypes>(r => new LoadedVersionedTypes(
                _jobTypes,
                _commandTypes,
                _aggregateEventTypes),
                Lifetime.Singleton);
        }

        public IRootResolver CreateResolver(bool validateRegistrations = true)
        {
            return _lazyRegistrationFactory.Value.CreateResolver(validateRegistrations);
        }
    }
}