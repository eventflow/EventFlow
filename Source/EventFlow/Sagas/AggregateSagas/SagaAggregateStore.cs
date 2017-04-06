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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;

namespace EventFlow.Sagas.AggregateSagas
{
    public class SagaAggregateStore : ISagaStore
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public SagaAggregateStore(
            IAggregateStore aggregateStore,
            IMemoryCache memoryCache)
        {
            _aggregateStore = aggregateStore;
            _memoryCache = memoryCache;
        }

        public async Task<TSaga> UpdateAsync<TSaga>(
            ISagaId sagaId,
            SagaDetails sagaDetails,
            ISourceId sourceId,
            Func<TSaga, CancellationToken, Task> updateSaga,
            CancellationToken cancellationToken)
            where TSaga : ISaga
        {
            var saga = default(TSaga);

            var storeAggregateSagaAsync = await GetUpdateAsync(
                sagaDetails.SagaType,
                cancellationToken)
                .ConfigureAwait(false);

            var domainEvents = await storeAggregateSagaAsync(
                sagaId,
                sourceId,
                async (s, c) =>
                    {
                        var specificSaga = (TSaga)s;
                        await updateSaga(specificSaga, c).ConfigureAwait(false);
                        saga = specificSaga;
                    },
                cancellationToken)
                .ConfigureAwait(false);

            return domainEvents.Any()
                ? saga
                : default(TSaga);
        }

        private async Task<Func<ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>> GetUpdateAsync(
            Type sagaType,
            CancellationToken cancellationToken)
        {
            var value = await _memoryCache.GetOrAddAsync(
                CacheKey.With(GetType(), sagaType.GetCacheKey()), 
                TimeSpan.FromDays(1),
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
                    return Task.FromResult<Func<ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>>(
                        (id, sid, u, c) => (Task<IReadOnlyCollection<IDomainEvent>>)genericMethodInfo.Invoke(this, new object[] { id, sid, u, c }));
                },
                cancellationToken)
                .ConfigureAwait(false);

            return value;
        }

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