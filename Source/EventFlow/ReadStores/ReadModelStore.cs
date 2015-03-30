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
using EventFlow.Logs;

namespace EventFlow.ReadStores
{
    public abstract class ReadModelStore<TAggregate, TReadModel> : IReadModelStore<TAggregate>
        where TAggregate : IAggregateRoot
        where TReadModel : IReadModel
    {
        private static readonly ConcurrentDictionary<Type, Action<TReadModel, IReadModelContext, IDomainEvent>> ApplyMethods = new ConcurrentDictionary<Type, Action<TReadModel, IReadModelContext, IDomainEvent>>();

        protected ILog Log { get; private set; }

        public abstract Task UpdateReadModelAsync(string aggregateId, IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken);

        protected ReadModelStore(ILog log)
        {
            Log = log;
        }

        protected void ApplyEvents(TReadModel readModel, IEnumerable<IDomainEvent> domainEvents)
        {
            var readModelType = typeof(TReadModel);
            var readModelContextType = typeof(IReadModelContext);
            var readModelContext = new ReadModelContext();

            foreach (var domainEvent in domainEvents)
            {
                var applyMethod = ApplyMethods.GetOrAdd(
                    domainEvent.EventType,
                    t =>
                        {
                            var domainEventType = typeof(IDomainEvent<>).MakeGenericType(t);
                            var methodInfo = readModelType.GetMethod("Apply", new[] { readModelContextType, domainEventType });
                            return  methodInfo == null
                                ? (r ,c, e) => Log.Warning("Read model '{0}' does not handle event '{1}'", readModelType.Name, t.Name)
                                : (Action<TReadModel, IReadModelContext, IDomainEvent>)((r, c, e) => methodInfo.Invoke(r, new object[] { c, e }));
                        });
                applyMethod(readModel, readModelContext, domainEvent);
            }
        }
    }
}
