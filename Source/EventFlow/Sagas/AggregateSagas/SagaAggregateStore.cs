// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Core.Caching;
using EventFlow.Extensions;

namespace EventFlow.Sagas.AggregateSagas
{
    public class SagaAggregateStore : SagaStore
    {
        private readonly IResolver _resolver;
        private readonly IAggregateStore _aggregateStore;
        private readonly IMemoryCache _memoryCache;

        public SagaAggregateStore(
            IResolver resolver,
            IAggregateStore aggregateStore,
            IMemoryCache memoryCache)
        {
            _resolver = resolver;
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

            var storeAggregateSagaAsync = await GetUpdateAsync(
                    sagaType,
                    cancellationToken)
                .ConfigureAwait(false);

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

            var commandBus = _resolver.Resolve<ICommandBus>();
            await saga.PublishAsync(commandBus, cancellationToken).ConfigureAwait(false);

            return saga;
        }

        private async Task<Func<SagaAggregateStore, ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>> GetUpdateAsync(
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
                    return Task.FromResult<Func<SagaAggregateStore, ISagaId, ISourceId, Func<ISaga, CancellationToken, Task>, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>>(
                        (sas, id, sid, u, c) => (Task<IReadOnlyCollection<IDomainEvent>>)genericMethodInfo.Invoke(sas, new object[] { id, sid, u, c }));
                },
                cancellationToken)
                .ConfigureAwait(false);

            return value;
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