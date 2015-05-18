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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;

namespace EventFlow.ReadStores
{
    public class ReadModelFactory : IReadModelFactory
    {
        private static readonly ConcurrentDictionary<Type, Action<IReadModel, IReadModelContext, IDomainEvent>> ApplyMethods = new ConcurrentDictionary<Type, Action<IReadModel, IReadModelContext, IDomainEvent>>();

        public Task<TReadModel> CreateReadModelAsync<TReadModel>(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken)
            where TReadModel : IReadModel, new()
        {
            return CreateReadModelAsync(domainEvents, readModelContext, () => new TReadModel(), cancellationToken);
        }

        public async Task<TReadModel> CreateReadModelAsync<TReadModel>(
            IReadOnlyCollection<IDomainEvent> domainEvents,
            IReadModelContext readModelContext,
            Func<TReadModel> readModelCreator,
            CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            var readModel = readModelCreator();
            await UpdateReadModelAsync(readModel, domainEvents, readModelContext, cancellationToken).ConfigureAwait(false);
            return readModel;
        }

        public Task<bool> UpdateReadModelAsync<TReadModel>(
            TReadModel readModel,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            var readModelContextType = typeof(IReadModelContext);
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
                                : (Action<IReadModel, IReadModelContext, IDomainEvent>)((r, c, e) => methodInfo.Invoke(r, new object[] { c, e }));
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
