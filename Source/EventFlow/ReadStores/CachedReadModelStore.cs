// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace EventFlow.ReadStores
{
    public abstract class CachedReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
    {
        private readonly IMemoryCache _memoryCache;

        protected ILogger Logger { get; }

        protected CachedReadModelStore(
            ILogger logger,
            IMemoryCache memoryCache)
        {
            Logger = logger;
            this._memoryCache = memoryCache;
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
        {
            return await _memoryCache.GetOrCreateAsync(CacheKey.With(this.GetType(), id), entry => 
            { 
                entry.SetSlidingExpiration(TimeSpan.FromMinutes(30)); //Make configurable
                return GetEntryAsync(id, cancellationToken); 
            });
        }
        public async Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            _memoryCache.Remove(CacheKey.With(this.GetType(), id));

            await DeleteEntryAsync(id, cancellationToken);
        }

        public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates, 
            IReadModelContextFactory readModelContextFactory, 
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, 
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, 
            CancellationToken cancellationToken)
        {
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModelWithCaching = (context, events, envelope, token) => updateReadModel(context, events, envelope, token)
                .ContinueWith(result =>
                {
                    var cacheNonFaulted = !result.IsFaulted;
                    if (cacheNonFaulted)
                    {
                        _memoryCache.Set(CacheKey.With(result.Result.Envelope.ReadModelId), result.Result.Envelope);
                    }

                    return result.Result;
                });

            await UpdateEntryAsync(readModelUpdates, readModelContextFactory, updateReadModelWithCaching, cancellationToken);
        }

        public abstract Task<ReadModelEnvelope<TReadModel>> GetEntryAsync(
            string id,
            CancellationToken cancellationToken);

        public abstract Task DeleteEntryAsync(
            string id,
            CancellationToken cancellationToken);

        public abstract Task DeleteAllAsync(
            CancellationToken cancellationToken);

        public abstract Task UpdateEntryAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken);
    }
}