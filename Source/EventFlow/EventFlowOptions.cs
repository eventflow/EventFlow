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
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
using EventFlow.Extensions;
using EventFlow.Jobs;
using EventFlow.Logs;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.Sagas;
using EventFlow.Sagas.AggregateSagas;
using EventFlow.Snapshots;
using EventFlow.Snapshots.Stores;
using EventFlow.Snapshots.Stores.Null;
using EventFlow.Subscribers;
using Microsoft.Extensions.DependencyInjection;

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

        private EventFlowOptions(IServiceCollection serviceCollection)
        {
            Register(serviceCollection);
        }

        public static IEventFlowOptions New(IServiceCollection serviceCollection) => new EventFlowOptions(serviceCollection);

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

        public IEventFlowOptions RegisterServices(Action<IServiceCollection> action)
        {
            throw new NotImplementedException();
        }

        private void Register(IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<ILog, ConsoleLog>();
            serviceCollection.AddTransient<IEventStore, EventStoreBase>();
            serviceCollection.AddSingleton<IEventPersistence, InMemoryEventPersistence>();
            serviceCollection.AddTransient<ICommandBus, CommandBus>();
            serviceCollection.AddTransient<IAggregateStore, AggregateStore>();
            serviceCollection.AddTransient<ISnapshotStore, SnapshotStore>();
            serviceCollection.AddTransient<ISnapshotSerilizer, SnapshotSerilizer>();
            serviceCollection.AddTransient<ISnapshotPersistence, NullSnapshotPersistence>();
            serviceCollection.AddTransient<ISnapshotUpgradeService, SnapshotUpgradeService>();
            serviceCollection.AddSingleton<ISnapshotDefinitionService, SnapshotDefinitionService>();
            serviceCollection.AddTransient<IReadModelPopulator, ReadModelPopulator>();
            serviceCollection.AddTransient<IEventJsonSerializer, EventJsonSerializer>();
            serviceCollection.AddSingleton<IEventDefinitionService, EventDefinitionService>();
            serviceCollection.AddTransient<IQueryProcessor, QueryProcessor>();
            serviceCollection.AddSingleton<IJsonSerializer, JsonSerializer>();
            serviceCollection.AddTransient<IJsonOptions, JsonOptions>();
            serviceCollection.AddTransient<IJobScheduler, InstantJobScheduler>();
            serviceCollection.AddTransient<IJobRunner, JobRunner>();
            serviceCollection.AddSingleton<IJobDefinitionService, JobDefinitionService>();
            serviceCollection.AddTransient<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>();
            serviceCollection.AddSingleton<IEventUpgradeManager, EventUpgradeManager>();
            serviceCollection.AddTransient<IAggregateFactory, AggregateFactory>();
            serviceCollection.AddTransient<IReadModelDomainEventApplier, ReadModelDomainEventApplier>();
            serviceCollection.AddTransient<IDomainEventPublisher, DomainEventPublisher>();
            serviceCollection.AddTransient<ISerializedCommandPublisher, SerializedCommandPublisher>();
            serviceCollection.AddSingleton<ICommandDefinitionService, CommandDefinitionService>();
            serviceCollection.AddTransient<IDispatchToEventSubscribers, DispatchToEventSubscribers>();
            serviceCollection.AddSingleton<IDomainEventFactory, DomainEventFactory>();
            serviceCollection.AddSingleton<ISagaDefinitionService, SagaDefinitionService>();
            serviceCollection.AddTransient<ISagaStore, SagaAggregateStore>();
            serviceCollection.AddTransient<ISagaErrorHandler, SagaErrorHandler>();
            serviceCollection.AddTransient<IDispatchToSagas, DispatchToSagas>();
            serviceCollection.AddTransient(typeof(ISagaUpdater<,,,>), typeof(SagaUpdater<,,,>));
            serviceCollection.AddTransient<IEventFlowConfiguration>(_ => _eventFlowConfiguration);
            serviceCollection.AddTransient<ICancellationConfiguration>(_ => _eventFlowConfiguration);
            serviceCollection.AddTransient(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));
            serviceCollection.AddSingleton(typeof(IReadModelFactory<>), typeof(ReadModelFactory<>));
            serviceCollection.AddTransient<IBootstrap, DefinitionServicesInitilizer>();
            serviceCollection.AddSingleton<ILoadedVersionedTypes>(r => new LoadedVersionedTypes(
                _jobTypes,
                _commandTypes,
                _aggregateEventTypes,
                _sagaTypes,
                _snapshotTypes));
        }
    }
}
