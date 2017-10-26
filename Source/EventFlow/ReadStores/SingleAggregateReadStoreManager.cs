﻿// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public class SingleAggregateReadStoreManager<TReadModelStore, TReadModel> : ReadStoreManager<TReadModelStore, TReadModel>
        where TReadModelStore : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        public SingleAggregateReadStoreManager(
            ILog log,
            IResolver resolver,
            TReadModelStore readModelStore,
            IReadModelDomainEventApplier readModelDomainEventApplier,
            IReadModelFactory<TReadModel> readModelFactory)
            : base(log, resolver, readModelStore, readModelDomainEventApplier, readModelFactory)
        {
        }

        protected override IReadOnlyCollection<ReadModelUpdate> BuildReadModelUpdates(
            IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            return domainEvents
                .GroupBy(d => d.GetIdentity().Value)
                .Select(g => new ReadModelUpdate(g.Key, g.OrderBy(d => d.AggregateSequenceNumber).ToList()))
                .ToList();
        }

        protected override async Task<ReadModelEnvelope<TReadModel>> UpdateAsync(
            IReadModelContext readModelContext,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            ReadModelEnvelope<TReadModel> readModelEnvelope,
            CancellationToken cancellationToken)
        {
            var readModel = readModelEnvelope.ReadModel ?? await ReadModelFactory.CreateAsync(readModelEnvelope.ReadModelId, cancellationToken).ConfigureAwait(false);

            await ReadModelDomainEventApplier.UpdateReadModelAsync(readModel, domainEvents, readModelContext, cancellationToken).ConfigureAwait(false);

            var readModelVersion = domainEvents.Max(e => e.AggregateSequenceNumber);

            return ReadModelEnvelope<TReadModel>.With(readModelEnvelope.ReadModelId, readModel, readModelVersion);
        }
    }
}