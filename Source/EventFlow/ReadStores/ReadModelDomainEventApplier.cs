// The MIT License (MIT)
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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;

namespace EventFlow.ReadStores
{
    public class ReadModelDomainEventApplier : IReadModelDomainEventApplier
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<IReadModel, IReadModelContext, IDomainEvent>>> ApplyMethods = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Action<IReadModel, IReadModelContext, IDomainEvent>>>();

        public Task<bool> UpdateReadModelAsync<TReadModel>(
            TReadModel readModel,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken)
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            var appliedAny = false;

            foreach (var domainEvent in domainEvents)
            {
                var applyMethods = ApplyMethods.GetOrAdd(
                    readModelType,
                    t => new ConcurrentDictionary<Type, Action<IReadModel, IReadModelContext, IDomainEvent>>());
                var applyMethod = applyMethods.GetOrAdd(
                    domainEvent.EventType,
                    t =>
                        {
                            var domainEventType = typeof(IDomainEvent<,,>).MakeGenericType(domainEvent.AggregateType, domainEvent.GetIdentity().GetType(), t);

                            var methodSignature = new[] {typeof(IReadModelContext), domainEventType};
                            var methodInfo = readModelType.GetTypeInfo().GetMethod("Apply", methodSignature);

                            return methodInfo == null
                                ? null
                                : ReflectionHelper.CompileMethodInvocation<Action<IReadModel, IReadModelContext, IDomainEvent>>(readModelType, "Apply", methodSignature);
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