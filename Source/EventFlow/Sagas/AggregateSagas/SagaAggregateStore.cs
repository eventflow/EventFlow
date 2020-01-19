// The MIT License (MIT)
//
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using EventFlow.Core;
using EventFlow.Extensions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EventFlow.Sagas.AggregateSagas
{
    public class SagaAggregateStore : SagaStore
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public SagaAggregateStore(
            IServiceProvider serviceProvider,
            IAggregateStore aggregateStore,
            IMemoryCache memoryCache)
        {
            _serviceProvider = serviceProvider;
            _aggregateStore = aggregateStore;
            _memoryCache = memoryCache;
        }

        public override async Task<ISaga> UpdateAsync(
            ISagaId sagaId,
            Type sagaType,
            ISourceId sourceId,
            Func<ISaga, CancellationToken, Task> updateSaga,
            CancellationToken cancellationToken)
        {
            var saga = null as ISaga;

            var storeAggregateSagaAsync = GetUpdate(sagaType);

            await storeAggregateSagaAsync(
                    this,
                    sagaId,
                    sourceId,
                    async (s, c) =>
                        {
                            await updateSaga(s, c).ConfigureAwait(false);
                            saga = s;
                        },
                    cancellationToken)
                .ConfigureAwait(false);

            if (saga is null)
            {
                return null;
            }

            var commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
            await saga.PublishAsync(commandBus, cancellationToken).ConfigureAwait(false);

            return saga;
        }

        private Func<SagaAggregateStore, ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>> GetUpdate(
            Type sagaType)
        {
            return _memoryCache.GetOrCreate(
                CacheKey.Get(GetType(), sagaType), 
                _ =>
                {
                    var aggregateRootType = sagaType
                        .GetTypeInfo()
                        .GetInterfaces()
                        .FirstOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

                    if (aggregateRootType == null)
                        throw new ArgumentException($"Saga '{sagaType.PrettyPrint()}' is not a aggregate root");

                    var methodInfo = GetType().GetTypeInfo().GetMethod(nameof(UpdateAggregateAsync));
                    var identityType = aggregateRootType.GetTypeInfo().GetGenericArguments()[0];
                    var genericMethodInfo = methodInfo.MakeGenericMethod(sagaType, identityType);
                    return (Func<SagaAggregateStore, ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>)(
                        (sas, id, sid, u, c) => (Task<IReadOnlyCollection<IDomainEvent>>)genericMethodInfo.Invoke(sas, new object[] { id, sid, u, c }));
                });
        }

        // ReSharper disable once MemberCanBePrivate.Global
        public async Task<IReadOnlyCollection<IDomainEvent>> UpdateAggregateAsync<TAggregate, TIdentity>(
            TIdentity id,
            ISourceId sourceId,
            Func<TAggregate, CancellationToken, Task> updateAggregate,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>, ISaga
            where TIdentity : IIdentity
        {
            return await _aggregateStore.UpdateAsync(
                id,
                sourceId,
                updateAggregate,
                cancellationToken)
                .ConfigureAwait(false);
        }
    }
}
