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
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration.Serialization;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EventStores;
using EventFlow.EventStores.InMemory;
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
using Microsoft.Extensions.DependencyInjection.Extensions;

// ReSharper disable UnusedMethodReturnValue.Local

namespace EventFlow.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IEventFlowBuilder AddEventFlow(
            this IServiceCollection serviceCollection,
            Action<EventFlowOptions> configure = null)
        {
            if (serviceCollection == null)
            {
                throw new ArgumentNullException(nameof(serviceCollection));
            }

            var builder = new EventFlowBuilder(serviceCollection);
            if (configure != null)
            {
                builder.Services.Configure(configure);
            }

            serviceCollection
                .TryAddEventFlow();

            return builder;
        }

        private static IServiceCollection TryAddEventFlow(this IServiceCollection serviceCollection)
        {
            // Add transient services
            serviceCollection.TryAddTransient<IEventStore, EventStoreBase>();
            serviceCollection.TryAddTransient<ICommandBus, CommandBus>();
            serviceCollection.TryAddTransient<IAggregateStore, AggregateStore>();
            serviceCollection.TryAddTransient<ISnapshotStore, SnapshotStore>();
            serviceCollection.TryAddTransient<ISnapshotSerilizer, SnapshotSerilizer>();
            serviceCollection.TryAddTransient<ISnapshotPersistence, NullSnapshotPersistence>();
            serviceCollection.TryAddTransient<ISnapshotUpgradeService, SnapshotUpgradeService>();
            serviceCollection.TryAddTransient<IEventJsonSerializer, EventJsonSerializer>();
            serviceCollection.TryAddTransient<IReadModelPopulator, ReadModelPopulator>();
            serviceCollection.TryAddTransient<IQueryProcessor, QueryProcessor>();
            serviceCollection.TryAddTransient<IJsonOptions, JsonOptions>();
            serviceCollection.TryAddTransient<IJobScheduler, InstantJobScheduler>();
            serviceCollection.TryAddTransient<IJobRunner, JobRunner>();
            serviceCollection.TryAddTransient<IAggregateFactory, AggregateFactory>();
            serviceCollection.TryAddTransient<IDomainEventPublisher, DomainEventPublisher>();
            serviceCollection.TryAddTransient<ISerializedCommandPublisher, SerializedCommandPublisher>();
            serviceCollection.TryAddTransient<IOptimisticConcurrencyRetryStrategy, OptimisticConcurrencyRetryStrategy>();
            serviceCollection.TryAddTransient<IDispatchToEventSubscribers, DispatchToEventSubscribers>();
            serviceCollection.TryAddTransient<ISagaStore, SagaAggregateStore>();
            serviceCollection.TryAddTransient<ISagaErrorHandler, SagaErrorHandler>();
            serviceCollection.TryAddTransient<IDispatchToSagas, DispatchToSagas>();
            serviceCollection.TryAddTransient(typeof(ISagaUpdater<,,,>), typeof(SagaUpdater<,,,>));
            serviceCollection.TryAddTransient(typeof(ITransientFaultHandler<>), typeof(TransientFaultHandler<>));

            // Add type definition singleton services
            serviceCollection.TryAddSingleton<ISnapshotDefinitionService, SnapshotDefinitionService>();
            serviceCollection.TryAddSingleton<IEventDefinitionService, EventDefinitionService>();
            serviceCollection.TryAddSingleton<IJobDefinitionService, JobDefinitionService>();
            serviceCollection.TryAddSingleton<ICommandDefinitionService, CommandDefinitionService>();
            serviceCollection.TryAddSingleton<ISagaDefinitionService, SagaDefinitionService>();

            // Add singleton services
            serviceCollection.TryAddSingleton<IReadModelDomainEventApplier, ReadModelDomainEventApplier>();
            serviceCollection.TryAddSingleton<ILog, Logger>();
            serviceCollection.TryAddSingleton<IEventPersistence, InMemoryEventPersistence>();
            serviceCollection.TryAddSingleton<IJsonSerializer, JsonSerializer>();
            serviceCollection.TryAddSingleton<IEventUpgradeManager, EventUpgradeManager>();
            serviceCollection.TryAddSingleton<IDomainEventFactory, DomainEventFactory>();
            serviceCollection.TryAddSingleton(typeof(IReadModelFactory<>), typeof(ReadModelFactory<>));

            return serviceCollection;
        }
    }
}
