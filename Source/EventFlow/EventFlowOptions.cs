// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
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
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.Bootstraps;
using EventFlow.Configuration.Cancellation;
using EventFlow.Configuration.Serialization;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Core.IoC;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using EventFlow.Jobs;
using EventFlow.Logs;
using EventFlow.Provided;
using EventFlow.PublishRecovery;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Sagas;
using EventFlow.Sagas.AggregateSagas;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using EventFlow.Snapshots.Stores.Null;
using EventFlow.Subscribers;

namespace EventFlow
{
    public class EventFlowOptions : IEventFlowOptions
    {
        private readonly List<Type> _aggregateEventTypes = new List<Type>();
        private readonly List<Type> _sagaTypes = new List<Type>(); 
        private readonly List<Type> _commandTypes = new List<Type>();
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();
        private readonly List<Type> _jobTypes = new List<Type>();
        private readonly List<Type> _snapshotTypes = new List<Type>(); 
        private Lazy<IServiceRegistration> _lazyRegistrationFactory;

        private EventFlowOptions()
        {
            UseServiceRegistration(new EventFlowIoCServiceRegistration());

            ModuleRegistration = new ModuleRegistration(this);
            ModuleRegistration.Register<ProvidedJobsModule>();
        }

        public static IEventFlowOptions New => new EventFlowOptions();

        public IModuleRegistration ModuleRegistration { get; }

        public IEventFlowOptions ConfigureOptimisticConcurrentcyRetry(int retries, TimeSpan delayBeforeRetry)
        {
            _eventFlowConfiguration.NumberOfRetriesOnOptimisticConcurrencyExceptions = retries;
            _eventFlowConfiguration.DelayBeforeRetryOnOptimisticConcurrencyExceptions = delayBeforeRetry;
            return this;
        }

        public IEventFlowOptions ConfigureThrowSubscriberExceptions(bool shouldThrow)
        {
            _eventFlowConfiguration.ThrowSubscriberExceptions = shouldThrow;
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
                if (!typeof(IAggregateEvent).GetTypeInfo().IsAssignableFrom(aggregateEventType))
                {
                    throw new ArgumentException($"Type {aggregateEventType.PrettyPrint()} is not a {typeof(IAggregateEvent).PrettyPrint()}");
                }
                _aggregateEventTypes.Add(aggregateEventType);
            }
            return this;
        }

        public IEventFlowOptions AddSagas(IEnumerable<Type> sagaTypes)
        {
            foreach (var sagaType in sagaTypes)
            {
                if (!typeof(ISaga).GetTypeInfo().IsAssignableFrom(sagaType))
                {
                    throw new ArgumentException($"Type {sagaType.PrettyPrint()} is not a {typeof(ISaga).PrettyPrint()}");
                }
                _sagaTypes.Add(sagaType);
            }
            return this;
        }

        public IEventFlowOptions AddCommands(IEnumerable<Type> commandTypes)
        {
            foreach (var commandType in commandTypes)
            {
                if (!typeof(ICommand).GetTypeInfo().IsAssignableFrom(commandType))
                {
                    throw new ArgumentException($"Type {commandType.PrettyPrint()} is not a {typeof(ICommand).PrettyPrint()}");
                }
                _commandTypes.Add(commandType);
            }
            return this;
        }

        public IEventFlowOptions AddJobs(IEnumerable<Type> jobTypes)
        {
            foreach (var jobType in jobTypes)
            {
                if (!typeof(IJob).GetTypeInfo().IsAssignableFrom(jobType))
                {
                    throw new ArgumentException($"Type {jobType.PrettyPrint()} is not a {typeof(IJob).PrettyPrint()}");
                }
                _jobTypes.Add(jobType);
            }
            return this;
        }

        public IEventFlowOptions AddSnapshots(IEnumerable<Type> snapshotTypes)
        {
            foreach (var snapshotType in snapshotTypes)
            {
                if (!typeof(ISnapshot).GetTypeInfo().IsAssignableFrom(snapshotType))
                {
                    throw new ArgumentException($"Type {snapshotType.PrettyPrint()} is not a {typeof(ISnapshot).PrettyPrint()}");
                }
                _snapshotTypes.Add(snapshotType);
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
            serviceRegistration.Register<ILog, ConsoleLog>();
            serviceRegistration.Register<IAggregateStoreResilienceStrategy, NoAggregateStoreResilienceStrategy>();
            serviceRegistration.Register<IDispatchToReadStoresResilienceStrategy, NoDispatchToReadStoresResilienceStrategy>();
            serviceRegistration.Register<ISagaUpdateResilienceStrategy, NoSagaUpdateResilienceStrategy>();
            serviceRegistration.Register<IDispatchToSubscriberResilienceStrategy, NoDispatchToSubscriberResilienceStrategy>();
            serviceRegistration.Register<IDispatchToReadStores, DispatchToReadStores>();
            serviceRegistration.Register<IEventStore, EventStoreBase>();
            serviceRegistration.Register<IEventPersistence, InMemoryEventPersistence>(Lifetime.Singleton);
            serviceRegistration.Register<ICommandBus, CommandBus>();
            serviceRegistration.Register<IAggregateStore, AggregateStore>();
            serviceRegistration.Register<ISnapshotStore, SnapshotStore>();
            serviceRegistration.Register<ISnapshotSerilizer, SnapshotSerilizer>();
            serviceRegistration.Register<ISnapshotPersistence, NullSnapshotPersistence>();
            serviceRegistration.Register<ISnapshotUpgradeService, SnapshotUpgradeService>();
            serviceRegistration.Register<ISnapshotDefinitionService, SnapshotDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<IReadModelPopulator, ReadModelPopulator>();
            serviceRegistration.Register<IEventJsonSerializer, EventJsonSerializer>();
            serviceRegistration.Register<IEventDefinitionService, EventDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<IQueryProcessor, QueryProcessor>();
            serviceRegistration.Register<IJsonSerializer, JsonSerializer>(Lifetime.Singleton);
            serviceRegistration.Register<IJsonOptions, JsonOptions>();
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
            serviceRegistration.Register<ISagaDefinitionService, SagaDefinitionService>(Lifetime.Singleton);
            serviceRegistration.Register<ISagaStore, SagaAggregateStore>();
            serviceRegistration.Register<ISagaErrorHandler, SagaErrorHandler>();
            serviceRegistration.Register<IDispatchToSagas, DispatchToSagas>();
#if NET452
            serviceRegistration.Register<IMemoryCache, MemoryCache>(Lifetime.Singleton);
#else
            serviceRegistration.Register<IMemoryCache, DictionaryMemoryCache>(Lifetime.Singleton);
#endif
            serviceRegistration.RegisterGeneric(typeof(ISagaUpdater<,,,>), typeof(SagaUpdater<,,,>));
            serviceRegistration.Register<IEventFlowConfiguration>(_ => _eventFlowConfiguration);
            serviceRegistration.Register<ICancellationConfiguration>(_ => _eventFlowConfiguration);
            serviceRegistration.RegisterGeneric(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            serviceRegistration.RegisterGeneric(typeof(IReadModelFactory<>), typeof(ReadModelFactory<>), Lifetime.Singleton);
            serviceRegistration.Register<IBootstrap, DefinitionServicesInitilizer>();
            serviceRegistration.Register(_ => ModuleRegistration, Lifetime.Singleton);
            serviceRegistration.Register<ILoadedVersionedTypes>(r => new LoadedVersionedTypes(
                _jobTypes,
                _commandTypes,
                _aggregateEventTypes,
                _sagaTypes,
                _snapshotTypes),
                Lifetime.Singleton);
        }

        public IRootResolver CreateResolver(bool validateRegistrations = true)
        {
            return _lazyRegistrationFactory.Value.CreateResolver(validateRegistrations);
        }
    }
}