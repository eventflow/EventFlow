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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public abstract class MultiAggregateReadModelStore<TReadModel, TReadModelLocator> : ReadModelStore<TReadModel>
        where TReadModel : IReadModel
        where TReadModelLocator : IReadModelLocator
    {
        protected TReadModelLocator ReadModelLocator { get; private set; }

        protected MultiAggregateReadModelStore(
            ILog log,
            TReadModelLocator readModelLocator,
            IReadModelDomainEventApplier readModelDomainEventApplier)
            : base (log, readModelDomainEventApplier)
        {
            ReadModelLocator = readModelLocator;
        }

        public override Task ApplyDomainEventsAsync(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            var readModelUpdates = (
                from de in domainEvents
                let readModelIds = ReadModelLocator.GetReadModelIds(de)
                from rid in readModelIds
                group de by rid into g
                select new ReadModelUpdate(g.Key, g.ToList())
                ).ToList();

            var readModelContext = new ReadModelContext();

            return UpdateReadModelsAsync(readModelUpdates, readModelContext, cancellationToken);
        }

        public override Task ApplyDomainEventsAsync<TReadModelToPopulate>(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            return (typeof (TReadModel) == typeof (TReadModelToPopulate))
                ? ApplyDomainEventsAsync(domainEvents, cancellationToken)
                : Task.FromResult(0);
        }

        public abstract Task<TReadModel> GetByIdAsync(
			string id,
			CancellationToken cancellationToken);

        protected abstract Task UpdateReadModelsAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken);
    }
}
