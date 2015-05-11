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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public abstract class ReadModelStore<TReadModel, TReadModelLocator> : IReadModelStore
        where TReadModel : IReadModel
        where TReadModelLocator : IReadModelLocator
    {
        private static readonly ConcurrentDictionary<Type, Action<TReadModel, IReadModelContext, IDomainEvent>> ApplyMethods = new ConcurrentDictionary<Type, Action<TReadModel, IReadModelContext, IDomainEvent>>();

        protected ILog Log { get; private set; }
        protected TReadModelLocator ReadModelLocator { get; private set; }

        protected ReadModelStore(
            ILog log, TReadModelLocator readModelLocator)
        {
            Log = log;
            ReadModelLocator = readModelLocator;
        }

        public virtual Task ApplyDomainEventsAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken)
        {
            var readModelUpdates = (
                from de in domainEvents
                let readModelIds = ReadModelLocator.GetReadModelIds(de)
                from rid in readModelIds
                group de by rid into g
                select new ReadModelUpdate(g.Key, g.ToList())
                ).ToList();

            return UpdateReadModelsAsync(readModelUpdates, cancellationToken);
        }

        protected abstract Task UpdateReadModelsAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates, CancellationToken cancellationToken);

        protected virtual Task<bool> ApplyEventsAsync(TReadModel readModel, IEnumerable<IDomainEvent> domainEvents)
        {
            var readModelType = typeof(TReadModel);
            var readModelContextType = typeof(IReadModelContext);
            var readModelContext = new ReadModelContext();
            var appliedAny = false;

            foreach (var domainEvent in domainEvents)
            {
                var applyMethod = ApplyMethods.GetOrAdd(
                    domainEvent.EventType,
                    t =>
                        {
                            var domainEventType = typeof(IDomainEvent<,,>).MakeGenericType(domainEvent.AggregateType, domainEvent.GetIdentity().GetType(), t);
                            var methodInfo = readModelType.GetMethod("Apply", new[] { readModelContextType, domainEventType });
                            return methodInfo == null
                                ? null
                                : (Action<TReadModel, IReadModelContext, IDomainEvent>)((r, c, e) => methodInfo.Invoke(r, new object[] { c, e }));
                        });

                if (applyMethod != null)
                {
                    applyMethod(readModel, readModelContext, domainEvent);
                    appliedAny = true;
                }
            }

            return Task.FromResult(appliedAny);
        }
    }
}
