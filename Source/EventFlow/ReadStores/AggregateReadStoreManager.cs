// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public class AggregateReadStoreManager<TAggregate, TIdentity, TReadModelStore, TReadModel> : SingleAggregateReadStoreManager<TReadModelStore, TReadModel>
        where TAggregate : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
        where TReadModelStore : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        private readonly IEventStore _eventStore;

        public AggregateReadStoreManager(
            ILog log,
            IResolver resolver,
            TReadModelStore readModelStore,
            IReadModelDomainEventApplier readModelDomainEventApplier,
            IReadModelFactory<TReadModel> readModelFactory,
            IEventStore eventStore)
            : base(log, resolver, readModelStore, readModelDomainEventApplier, readModelFactory)
        {
            _eventStore = eventStore;
        }

        protected override async Task<ReadModelEnvelope<TReadModel>> UpdateAsync(
            IReadModelContext readModelContext,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            ReadModelEnvelope<TReadModel> readModelEnvelope,
            CancellationToken cancellationToken)
        {
            if (!domainEvents.Any()) throw new ArgumentException("No domain events");

            var expectedVersion = domainEvents.Min(d => d.AggregateSequenceNumber) - 1;
            var version = readModelEnvelope.Version;

            if (!version.HasValue || expectedVersion == version)
            {
                return await base.UpdateAsync(
                    readModelContext,
                    domainEvents,
                    readModelEnvelope,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            if (expectedVersion < version.Value)
            {
                throw new OptimisticConcurrencyException(
                    $"Read model '{readModelEnvelope.ReadModelId}' ({typeof(TReadModel).PrettyPrint()}) is already updated ({expectedVersion} < {version.Value})");
            }

            TReadModel readModel;
            
            if (readModelEnvelope.ReadModel == null)
            {
                readModel = await ReadModelFactory.CreateAsync(
                    readModelEnvelope.ReadModelId,
                    cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                readModel = readModelEnvelope.ReadModel;
            }

            // Apply ALL events
            var identity = domainEvents.Cast<IDomainEvent<TAggregate, TIdentity>>().First().AggregateIdentity;
            var missingEvents = await _eventStore.LoadEventsAsync<TAggregate, TIdentity>(
                identity,
                (int) version.Value,
                cancellationToken)
                .ConfigureAwait(false);

            await ReadModelDomainEventApplier.UpdateReadModelAsync(
                readModel,
                missingEvents,
                readModelContext,
                cancellationToken)
                .ConfigureAwait(false);

            version = domainEvents.Max(e => e.AggregateSequenceNumber);

            return ReadModelEnvelope<TReadModel>.With(readModelEnvelope.ReadModelId, readModel, version.Value);
        }
    }
}