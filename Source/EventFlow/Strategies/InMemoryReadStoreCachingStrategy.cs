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

using EventFlow.Core.Caching;
using EventFlow.ReadStores;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Strategies
{
    public class InMemoryReadStoreCachingStrategy : IReadStoreCachingStrategy
    {
        private string GenerateSeed => Guid.NewGuid().ToString();
        private readonly IMemoryCache memoryCache;
        private readonly IReadStoreCachingConfiguration cachingConfig;
        private CacheKey GenerateKey(Type readModel, string readModelId) => CacheKey.With(GetType(), currentSeed, readModel.ToString(), readModelId);
        private static string currentSeed;

        public InMemoryReadStoreCachingStrategy(IMemoryCache memoryCache, IReadStoreCachingConfiguration cachingConfig)
        {
            this.memoryCache = memoryCache;
            this.cachingConfig = cachingConfig;
            currentSeed = GenerateSeed;
        }

        public Task DeleteReadModel<TReadModel>(string readModelId, CancellationToken cancellationToken)
        {
            var cacheKey = GenerateKey(typeof(TReadModel), readModelId);
            memoryCache.Remove(cacheKey);

            return Task.CompletedTask;
        }

        public Task DeleteAllReadModels(CancellationToken cancellationToken)
        {
            currentSeed = GenerateSeed;
            return Task.CompletedTask;
        }

        public Task UpdateReadStoreModel<TReadModel>(IReadOnlyCollection<ReadModelUpdateResult<TReadModel>> updatedModels, CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            var createModels = updatedModels.Where(x => !x.Envelope.IsEmpty).ToList();
            var removedModels = updatedModels.Where(x => x.Envelope.IsEmpty).ToList();

            createModels.ForEach(model => memoryCache.Set(GenerateKey(typeof(TReadModel), model.Envelope.ReadModelId), model.Envelope, cachingConfig.ReadModelCachePeriodOnWrite));
            removedModels.ForEach(model => memoryCache.Remove(GenerateKey(typeof(TReadModel), model.Envelope.ReadModelId)));

            return Task.CompletedTask;
        }

        public Task<ReadModelEnvelope<TReadModel>> QueryReadStoreModel<TReadModel>(string readModelId, CancellationToken cancellationToken)
            where TReadModel : class, IReadModel
        {
            var cacheKey = GenerateKey(typeof(TReadModel), readModelId);
            var exists = memoryCache.TryGetValue(cacheKey, out ReadModelEnvelope<TReadModel> model);
            if (exists)
            {
                memoryCache.Set(cacheKey, model, cachingConfig.ReadModelCachePeriodOnRead);
            }

            var wrappedResult = exists ? model : ReadModelEnvelope<TReadModel>.Empty(readModelId);

            return Task.FromResult(wrappedResult);
        }
    }
}
