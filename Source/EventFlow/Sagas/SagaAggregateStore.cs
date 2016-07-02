// The MIT License (MIT)
//
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.Cache;
using EventFlow.Extensions;

namespace EventFlow.Sagas
{
    public class SagaAggregateStore : ISagaStore
    {
        private readonly IAggregateStore _aggregateStore;
        private readonly IInMemoryCache _inMemoryCache;

        public SagaAggregateStore(
            IAggregateStore aggregateStore,
            IInMemoryCache inMemoryCache)
        {
            _aggregateStore = aggregateStore;
            _inMemoryCache = inMemoryCache;
        }

        public async Task<ISaga> LoadAsync(
            ISagaId sagaId,
            SagaTypeDetails sagaTypeDetails,
            CancellationToken cancellationToken)
        {
            var loadAggregateSagaAsync = await GetLoadAsync(
                sagaTypeDetails.SagaType,
                cancellationToken)
                .ConfigureAwait(false);
            var saga = await loadAggregateSagaAsync(
                sagaId,
                cancellationToken)
                .ConfigureAwait(false);

            return saga;
        }

        public async Task StoreAsync(
            ISaga saga,
            ISourceId sourceId,
            CancellationToken cancellationToken)
        {
            var storeAggregateSagaAsync = await GetStoreAsync(
                saga.GetType(),
                cancellationToken)
                .ConfigureAwait(false);
            await storeAggregateSagaAsync(saga, sourceId, cancellationToken).ConfigureAwait(false);
        }

        private async Task<Func<ISagaId, CancellationToken, Task<ISaga>>> GetLoadAsync(
            Type sagaType,
            CancellationToken cancellationToken)
        {
            var value = await _inMemoryCache.GetOrAddAsync(
                $"sagastore-load:{sagaType.GetCacheKey()}",
                TimeSpan.FromHours(1),
                _ =>
                    {
                        var aggregateRootType = sagaType
                            .GetInterfaces()
                            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

                        if (aggregateRootType == null)
                            throw new ArgumentException($"Saga '{sagaType.PrettyPrint()}' is not a aggregate root");

                        var methodInfo = GetType().GetMethod(nameof(LoadAggregateSagaAsync));
                        var identityType = aggregateRootType.GetGenericArguments()[0];
                        var genericMethodInfo = methodInfo.MakeGenericMethod(sagaType, identityType);
                        return Task.FromResult<Func<ISagaId, CancellationToken, Task<ISaga>>>((i, c) => (Task<ISaga>)genericMethodInfo.Invoke(this, new object[] { i, c }));
                    },
                cancellationToken)
                .ConfigureAwait(false);

            return value;
        }

        private async Task<Func<ISaga, ISourceId, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>> GetStoreAsync(
            Type sagaType,
            CancellationToken cancellationToken)
        {
            var value = await _inMemoryCache.GetOrAddAsync(
                $"sagastore-store:{sagaType.GetCacheKey()}",
                TimeSpan.FromHours(1),
                _ =>
                    {
                        var aggregateRootType = sagaType
                            .GetInterfaces()
                            .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateRoot<>));

                        if (aggregateRootType == null)
                            throw new ArgumentException($"Saga '{sagaType.PrettyPrint()}' is not a aggregate root");

                        var methodInfo = GetType().GetMethod(nameof(StoreAggregateSagaAsync));
                        var identityType = aggregateRootType.GetGenericArguments()[0];
                        var genericMethodInfo = methodInfo.MakeGenericMethod(sagaType, identityType);

                        return Task.FromResult<Func<ISaga, ISourceId, CancellationToken, Task<IReadOnlyCollection<IDomainEvent>>>>(
                            (i, s, c) => (Task<IReadOnlyCollection<IDomainEvent>>)genericMethodInfo.Invoke(this, new object[] { i, s, c }));
                    },
                cancellationToken)
                .ConfigureAwait(false);

            return value;
        }

        public async Task<ISaga> LoadAggregateSagaAsync<TAggregate, TIdentity>(
            TIdentity id,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>, ISaga
            where TIdentity : IIdentity
        {
            return await _aggregateStore.LoadAsync<TAggregate, TIdentity>(id, cancellationToken).ConfigureAwait(false);
        }

        public async Task<IReadOnlyCollection<IDomainEvent>>  StoreAggregateSagaAsync<TAggregate, TIdentity>(
            TAggregate aggregate,
            ISourceId sourceId,
            CancellationToken cancellationToken)
            where TAggregate : IAggregateRoot<TIdentity>, ISaga
            where TIdentity : IIdentity
        {
            return await _aggregateStore.StoreAsync<TAggregate, TIdentity>(aggregate, sourceId, cancellationToken).ConfigureAwait(false);
        }
    }
}