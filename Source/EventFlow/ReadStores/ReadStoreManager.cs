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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Extensions;
using Microsoft.Extensions.Logging;

namespace EventFlow.ReadStores
{
    public abstract class ReadStoreManager<TReadModelStore, TReadModel> : IReadStoreManager<TReadModel>
        where TReadModelStore : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly Type StaticReadModelType = typeof(TReadModel);
        private static readonly ISet<Type> AggregateEventTypes;
        // ReSharper enable StaticMemberInGenericType

        protected ILogger Logger { get; }
        protected IServiceProvider ServiceProvider { get; }
        protected TReadModelStore ReadModelStore { get; }
        protected IReadModelDomainEventApplier ReadModelDomainEventApplier { get; }
        protected IReadModelFactory<TReadModel> ReadModelFactory { get; }

        public Type ReadModelType => StaticReadModelType;

        static ReadStoreManager()
        {
            var iAmReadModelForInterfaceTypes = StaticReadModelType
                .GetTypeInfo()
                .GetInterfaces()
                .Where(IsReadModelFor)
                .ToList();
            if (!iAmReadModelForInterfaceTypes.Any())
            {
                throw new ArgumentException(
                    $"Read model type '{StaticReadModelType.PrettyPrint()}' does not implement any '{typeof(IAmReadModelFor<,,>).PrettyPrint()}'");
            }

            AggregateEventTypes = new HashSet<Type>(iAmReadModelForInterfaceTypes.Select(i => i.GetTypeInfo().GetGenericArguments()[2]));
            if (AggregateEventTypes.Count != iAmReadModelForInterfaceTypes.Count)
            {
                throw new ArgumentException(
                    $"Read model type '{StaticReadModelType.PrettyPrint()}' implements ambiguous '{typeof(IAmReadModelFor<,,>).PrettyPrint()}' interfaces");
            }
        }

        private static bool IsReadModelFor(Type i)
        {
            if (!i.GetTypeInfo().IsGenericType)
            {
                return false;
            }
            
            var typeDefinition = i.GetGenericTypeDefinition();
            return typeDefinition == typeof(IAmReadModelFor<,,>);
        }

        protected ReadStoreManager(
            ILogger logger,
            IServiceProvider serviceProvider,
            TReadModelStore readModelStore,
            IReadModelDomainEventApplier readModelDomainEventApplier,
            IReadModelFactory<TReadModel> readModelFactory)
        {
            Logger = logger;
            ServiceProvider = serviceProvider;
            ReadModelStore = readModelStore;
            ReadModelDomainEventApplier = readModelDomainEventApplier;
            ReadModelFactory = readModelFactory;
        }

        public async Task UpdateReadStoresAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            var relevantDomainEvents = domainEvents
                .Where(e => AggregateEventTypes.Contains(e.EventType))
                .ToList();

            if (!relevantDomainEvents.Any())
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(
                        "None of these events was relevant for read model {ReadModelType}, skipping update: {DomainEventTypes}",
                        StaticReadModelType.PrettyPrint(),
                        domainEvents.Select(e => e.EventType.PrettyPrint()).ToList());
                }
                return;
            }

            if (Logger.IsEnabled(LogLevel.Trace))
            {
                Logger.LogTrace(
                    "Updating read model {ReadModelType} in store {ReadModelStoreType} with these events: {DomainEventTypes}",
                    StaticReadModelType.PrettyPrint(),
                    typeof(TReadModelStore).PrettyPrint(),
                    relevantDomainEvents.Select(e => e.ToString()));
            }

            var contextFactory = new ReadModelContextFactory(ServiceProvider);

            var readModelUpdates = BuildReadModelUpdates(relevantDomainEvents);

            if (!readModelUpdates.Any())
            {
                if (Logger.IsEnabled(LogLevel.Trace))
                {
                    Logger.LogTrace(
                        "No read model updates after building for read model {ReadModelType} in store {ReadModelStoreType} with these events: {DomainEventTypes}",
                        StaticReadModelType.PrettyPrint(),
                        typeof(TReadModelStore).PrettyPrint(),
                        relevantDomainEvents.Select(e => e.ToString()).ToList());
                }
                return;
            }

            await ReadModelStore.UpdateAsync(
                readModelUpdates,
                contextFactory,
                UpdateAsync,
                cancellationToken)
                .ConfigureAwait(false);
        }

        protected abstract IReadOnlyCollection<ReadModelUpdate> BuildReadModelUpdates(
            IReadOnlyCollection<IDomainEvent> domainEvents);

        protected abstract Task<ReadModelUpdateResult<TReadModel>> UpdateAsync(
            IReadModelContext readModelContext,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            ReadModelEnvelope<TReadModel> readModelEnvelope,
            CancellationToken cancellationToken);
    }
}
