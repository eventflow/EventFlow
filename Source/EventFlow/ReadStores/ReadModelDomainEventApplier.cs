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
        private const string ApplyMethodName = "ApplyAsync";

        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, ApplyMethod>> ApplyMethods =
            new ConcurrentDictionary<Type, ConcurrentDictionary<Type, ApplyMethod>>();

        public async Task<bool> UpdateReadModelAsync<TReadModel>(
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
                    t => new ConcurrentDictionary<Type, ApplyMethod>());
                var applyMethod = applyMethods.GetOrAdd(
                    domainEvent.EventType,
                    t =>
                    {
                        var identityType = domainEvent.GetIdentity().GetType();
                        var aggregateType = domainEvent.AggregateType;
                        var eventType = typeof(IDomainEvent<,,>).MakeGenericType(aggregateType, identityType, t);

                        // first try: does it implement the synchronous 'Apply' method?

                        var interfaceType = typeof(IAmReadModelFor<,,>).MakeGenericType(aggregateType, identityType, t);
                        var methodParams = new[] {typeof(IReadModelContext), eventType, typeof(CancellationToken)};
                        var methodInfo = GetMethod(readModelType, interfaceType, ApplyMethodName, methodParams);

                        if (methodInfo != null)
                        {
                            var method = ReflectionHelper
                                .CompileMethodInvocation<Func<IReadModel, IReadModelContext, IDomainEvent, CancellationToken, Task>>(methodInfo);
                            return new ApplyMethod(method);
                        }

                        // no matching 'Apply' method found

                        return null;
                    });

                if (applyMethod != null)
                {
                    await applyMethod.Apply(readModel, readModelContext, domainEvent, cancellationToken).ConfigureAwait(false);
                    appliedAny = true;
                }
            }

            return appliedAny;
        }

        private static MethodInfo GetMethod(Type instanceType, Type interfaceType, string name, Type[] parameters)
        {
            var methodInfo = instanceType
                .GetTypeInfo()
                .GetMethod(name, parameters);

            if (methodInfo != null)
            {
                return methodInfo;
            }

            var type = interfaceType.GetTypeInfo();
            return type.IsAssignableFrom(instanceType)
                ? type.GetMethod(name, parameters)
                : default;
        }

        private class ApplyMethod
        {
            private readonly Func<IReadModel, IReadModelContext, IDomainEvent, CancellationToken, Task> _method;

            public ApplyMethod(Func<IReadModel, IReadModelContext, IDomainEvent, CancellationToken, Task> method)
            {
                _method = method;
            }

            public Task Apply(IReadModel readModel, IReadModelContext context, IDomainEvent domainEvent,
                CancellationToken cancellationToken)
            {
                return _method(readModel, context, domainEvent, cancellationToken);
            }
        }
    }
}
