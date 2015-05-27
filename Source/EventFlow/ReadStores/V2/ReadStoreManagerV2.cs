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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.ReadStores.V2
{
    public abstract class ReadStoreManagerV2<TReadModelStore, TReadModel> : IReadStoreManager
        where TReadModelStore : IReadModelStoreV2<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly Type ReadModelType = typeof(TReadModel);
        private static readonly ISet<Type> AggregateTypes;
        private static readonly ISet<Type> DomainEventTypes;
        // ReSharper enable StaticMemberInGenericType

        protected ILog Log { get; private set; }
        protected TReadModelStore ReadModelStore { get; private set; }
        protected IReadModelDomainEventApplier ReadModelDomainEventApplier { get; private set; }

        protected ISet<Type> GetAggregateTypes() { return AggregateTypes; }
        protected ISet<Type> GetDomainEventTypes() { return DomainEventTypes; } 

        static ReadStoreManagerV2()
        {
            var iAmReadModelForInterfaceTypes = ReadModelType
                .GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof (IAmReadModelFor<,,>))
                .ToList();
            if (!iAmReadModelForInterfaceTypes.Any())
            {
                throw new ArgumentException(string.Format(
                    "Read model type '{0}' does not implement any 'IAmReadModelFor<>'",
                    ReadModelType.Name));
            }

            AggregateTypes = new HashSet<Type>(iAmReadModelForInterfaceTypes.Select(i => i.GetGenericArguments()[0]));
            DomainEventTypes = new HashSet<Type>(iAmReadModelForInterfaceTypes.Select(i =>
                {
                    var genericArguments = i.GetGenericArguments();
                    return typeof (IDomainEvent<,,>).MakeGenericType(genericArguments);
                }));
        }

        protected ReadStoreManagerV2(
            ILog log,
            TReadModelStore readModelStore,
            IReadModelDomainEventApplier readModelDomainEventApplier)
        {
            Log = log;
            ReadModelStore = readModelStore;
            ReadModelDomainEventApplier = readModelDomainEventApplier;
        }

        public async Task UpdateReadStoresAsync<TAggregate, TIdentity>(
            TIdentity id,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>
            where TIdentity : IIdentity
        {
            var aggregateType = typeof (TAggregate);
            if (!AggregateTypes.Contains(aggregateType))
            {
                Log.Verbose(() => string.Format(
                    "Read model does not care about aggregate '{0}' so skipping update, only these: {1}",
                    ReadModelType.Name,
                    string.Join(", ", AggregateTypes.Select(t => t.Name))
                    ));
                return;
            }

            var relevantDomainEvents = domainEvents
                .Where(e => DomainEventTypes.Contains(e.GetType()))
                .ToList();
            if (!relevantDomainEvents.Any())
            {
                Log.Verbose(() => string.Format(
                    "None of these events was relevant for read model '{0}', skipping update: {1}",
                    ReadModelType.Name,
                    string.Join(", ", domainEvents.Select(e => e.EventType.Name))
                    ));
                return;
            }

            var readModelContext = new ReadModelContext();

            await ReadModelStore.UpdateAsync(
                id.Value,
                relevantDomainEvents,
                readModelContext,
                UpdateAsync,
                cancellationToken)
                .ConfigureAwait(false);
        }

        protected abstract Task<ReadModelEnvelope<TReadModel>> UpdateAsync(
            IReadModelContext readModelContext,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            ReadModelEnvelope<TReadModel> readModelEnvelope,
            CancellationToken cancellationToken);
    }
}
