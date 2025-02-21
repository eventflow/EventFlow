// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.Cancellation;
using EventFlow.Configuration.EventNamingStrategy;
using EventFlow.Configuration.Serialization;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using EventFlow.Jobs;
using EventFlow.Provided.Jobs;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Sagas;
using EventFlow.Sagas.AggregateSagas;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using EventFlow.Snapshots.Stores.Null;
using EventFlow.Subscribers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace EventFlow
{
    public class EventFlowOptions : IEventFlowOptions
    {
        private readonly ConcurrentBag<Type> _aggregateEventTypes = new ConcurrentBag<Type>();
        private readonly ConcurrentBag<Type> _sagaTypes = new ConcurrentBag<Type>();
        private readonly ConcurrentBag<Type> _commandTypes = new ConcurrentBag<Type>();
        private readonly EventFlowConfiguration _eventFlowConfiguration = new EventFlowConfiguration();

        private readonly ConcurrentBag<Type> _jobTypes = new ConcurrentBag<Type>
            {
                typeof(PublishCommandJob),
                typeof(DispatchToAsynchronousEventSubscribersJob)
            };

        private readonly List<Type> _snapshotTypes = new List<Type>();

        public IServiceCollection ServiceCollection { get; }

        private EventFlowOptions(IServiceCollection serviceCollection)
        {
            ServiceCollection = serviceCollection;

            RegisterDefaults(ServiceCollection);
        }

        public static IEventFlowOptions New() =>
            new EventFlowOptions(
                new ServiceCollection()
                    .AddLogging(b => b
                        .SetMinimumLevel(LogLevel.Trace)
                        .AddConsole()));

        public static IEventFlowOptions New(IServiceCollection serviceCollection) => new EventFlowOptions(serviceCollection);

        public IEventFlowOptions ConfigureOptimisticConcurrencyRetry(int retries, TimeSpan delayBeforeRetry)
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

        private void RegisterDefaults(IServiceCollection serviceCollection)
        {
            serviceCollection.AddMemoryCache();

            RegisterObsoleteDefaults(serviceCollection);

            // Default no-op resilience strategies
            serviceCollection.TryAddTransient<IAggregateStoreResilienceStrategy, NoAggregateStoreResilienceStrategy>();
            serviceCollection.TryAddTransient<IDispatchToReadStoresResilienceStrategy, NoDispatchToReadStoresResilienceStrategy>();
            serviceCollection.TryAddTransient<ISagaUpdateResilienceStrategy, NoSagaUpdateResilienceStrategy>();
            serviceCollection.TryAddTransient<IDispatchToSubscriberResilienceStrategy, NoDispatchToSubscriberResilienceStrategy>();

            serviceCollection.TryAddTransient<IDispatchToReadStores, DispatchToReadStores>();
            serviceCollection.TryAddTransient<IEventStore, EventStoreBase>();
            serviceCollection.TryAddSingleton<IEventUpgradeContextFactory, EventUpgradeContextFactory>();
            serviceCollection.TryAddSingleton<IEventPersistence, InMemoryEventPersistence>();
            serviceCollection.TryAddTransient<ICommandBus, CommandBus>();
            serviceCollection.TryAddTransient<IAggregateStore, AggregateStore>();
            serviceCollection.TryAddTransient<ISnapshotStore, SnapshotStore>();
            serviceCollection.TryAddTransient<ISnapshotSerializer, SnapshotSerializer>();
            serviceCollection.TryAddTransient<ISnapshotPersistence, NullSnapshotPersistence>();
            serviceCollection.TryAddTransient<ISnapshotUpgradeService, SnapshotUpgradeService>();
            serviceCollection.TryAddTransient<IReadModelPopulator, ReadModelPopulator>();
            serviceCollection.TryAddTransient<IEventJsonSerializer, EventJsonSerializer>();
            serviceCollection.TryAddTransient<IQueryProcessor, QueryProcessor>();
            serviceCollection.TryAddSingleton<IJsonSerializer, JsonSerializer>();
            serviceCollection.TryAddTransient<IJsonOptions, JsonOptions>();
            serviceCollection.TryAddTransient<IJobScheduler, InstantJobScheduler>();
            serviceCollection.TryAddTransient<IJobRunner, JobRunner>();
            serviceCollection.TryAddTransient<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>();
            serviceCollection.TryAddSingleton<IEventUpgradeManager, EventUpgradeManager>();
            serviceCollection.TryAddTransient<IAggregateFactory, AggregateFactory>();
            serviceCollection.TryAddTransient<IReadModelDomainEventApplier, ReadModelDomainEventApplier>();
            serviceCollection.TryAddTransient<IDomainEventPublisher, DomainEventPublisher>();
            serviceCollection.TryAddTransient<ISerializedCommandPublisher, SerializedCommandPublisher>();
            serviceCollection.TryAddTransient<IDispatchToEventSubscribers, DispatchToEventSubscribers>();
            serviceCollection.TryAddSingleton<IDomainEventFactory, DomainEventFactory>();
            serviceCollection.TryAddTransient<ISagaStore, SagaAggregateStore>();
            serviceCollection.TryAddTransient<ISagaErrorHandler, SagaErrorHandler>();
            serviceCollection.TryAddTransient<IDispatchToSagas, DispatchToSagas>();
            serviceCollection.TryAddTransient(typeof(ISagaUpdater<,,,>), typeof(SagaUpdater<,,,>));
            serviceCollection.TryAddTransient<IEventFlowConfiguration>(_ => _eventFlowConfiguration);
            serviceCollection.TryAddTransient<ICancellationConfiguration>(_ => _eventFlowConfiguration);
            serviceCollection.TryAddTransient(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            serviceCollection.TryAddSingleton(typeof(IReadModelFactory<>), typeof(ReadModelFactory<>));
            serviceCollection.TryAddSingleton<Func<Type, ISagaErrorHandler>>(_ => __ => null);

            // Definition services
            serviceCollection.TryAddSingleton<IEventDefinitionService, EventDefinitionService>();
            serviceCollection.TryAddSingleton<ISnapshotDefinitionService, SnapshotDefinitionService>();
            serviceCollection.TryAddSingleton<IJobDefinitionService, JobDefinitionService>();
            serviceCollection.TryAddSingleton<ISagaDefinitionService, SagaDefinitionService>();
            serviceCollection.TryAddSingleton<ICommandDefinitionService, CommandDefinitionService>();

            serviceCollection.TryAddSingleton<ILoadedVersionedTypes>(r => new LoadedVersionedTypes(
                _jobTypes,
                _commandTypes,
                _aggregateEventTypes,
                _sagaTypes,
                _snapshotTypes));
            
            serviceCollection.TryAddTransient<IEventNamingStrategy, DefaultStrategy>();
        }

        private void RegisterObsoleteDefaults(IServiceCollection serviceCollection)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            serviceCollection.TryAddTransient<ISnapshotSerilizer, SnapshotSerilizer>();
#pragma warning restore CS0618 // Type or member is obsolete
        }
    }
}
