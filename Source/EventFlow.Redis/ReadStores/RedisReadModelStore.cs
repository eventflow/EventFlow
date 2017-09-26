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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Newtonsoft.Json;
using StackExchange.Redis;

namespace EventFlow.Redis.ReadStores
{
    public class RedisReadModelStore<TReadModel> : ReadModelStore<TReadModel>, IRedisReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IReadModelFactory<TReadModel> _readModelFactory;

        public RedisReadModelStore(
            ILog log,
            IConnectionMultiplexer connectionMultiplexer,
            IReadModelFactory<TReadModel> readModelFactory)
            : base(log)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _readModelFactory = readModelFactory;
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();
            var redisValue = (string) await database.StringGetAsync(id).ConfigureAwait(false);

            if (string.IsNullOrEmpty(redisValue))
            {
                return ReadModelEnvelope<TReadModel>.Empty(id);
            }

            var readModel = JsonConvert.DeserializeObject<TReadModel>(redisValue);
            return ReadModelEnvelope<TReadModel>.With(id, readModel);
        }

        public override async Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();
            foreach (var readModelUpdate in readModelUpdates)
            {
                var redisValue = (string) await database.StringGetAsync(readModelUpdate.ReadModelId).ConfigureAwait(false);
                var readModel = string.IsNullOrEmpty(redisValue)
                    ? await _readModelFactory.CreateAsync(readModelUpdate.ReadModelId, cancellationToken).ConfigureAwait(false)
                    : JsonConvert.DeserializeObject<TReadModel>(redisValue);
                var readModelEnvelope = ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, readModel);

                readModelEnvelope = await updateReadModel(
                    readModelContext,
                    readModelUpdate.DomainEvents,
                    readModelEnvelope,
                    cancellationToken)
                    .ConfigureAwait(false);

                redisValue = JsonConvert.SerializeObject(readModelEnvelope.ReadModel);
                await database.StringSetAsync(readModelUpdate.ReadModelId, redisValue).ConfigureAwait(false);
            }
        }

        public override async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var database = _connectionMultiplexer.GetDatabase();
            await database.KeyDeleteAsync(id).ConfigureAwait(false);
        }

        public override async Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            // TODO: This doesn't seem like a good idea
            
            var database = _connectionMultiplexer.GetDatabase();
            var server = _connectionMultiplexer.GetServer(_connectionMultiplexer.Configuration);
            foreach (var redisKey in server.Keys())
            {
                await database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
            }
        }
    }
}