// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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

using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore
{

    internal class RedisReadModelStore<TReadModel> : IReadModelStore<TReadModel>
        where TReadModel : RedisReadModel
    {
        private readonly IRedisCollection<TReadModel> _collection;
        private readonly IRedisHashBuilder _hashBuilder;
        private readonly ILogger<RedisReadModelStore<TReadModel>> _logger;
        private readonly IConnectionMultiplexer _multiplexer;
        private readonly RedisConnectionProvider _provider;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public RedisReadModelStore(RedisConnectionProvider redisConnectionProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler,
            IConnectionMultiplexer multiplexer, IRedisHashBuilder hashBuilder,
            ILogger<RedisReadModelStore<TReadModel>> logger)
        {
            _provider = redisConnectionProvider;
            _transientFaultHandler = transientFaultHandler;
            _multiplexer = multiplexer;
            _hashBuilder = hashBuilder;
            _logger = logger;
            _collection = redisConnectionProvider.RedisCollection<TReadModel>();
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var prefixedKey = new PrefixedKey(Constants.ReadModelPrefix, id);
            var first = await _collection.FindByIdAsync(prefixedKey).ConfigureAwait(false);
            if (first is null)
            {
                _logger.LogTrace("Failed to delete read model with id {Id} because it was not found", id);
                return;
            }

            await _collection.DeleteAsync(first).ConfigureAwait(false);
            _logger.LogTrace("Deleted Read model {Id}", id);
        }

        public Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(TReadModel));
            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace(
                    !result
                        ? "Failed to delete index and records of read model {Name}"
                        : "Deleted index and records of read model {Name}", typeof(TReadModel).PrettyPrint());

            }

            return Task.CompletedTask;
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
        {
            var rm = await _collection.FindByIdAsync(id).ConfigureAwait(false);
            _logger.LogTrace("Found read model with {Id}: {ReadModel}", id, rm);

            return rm is null ? ReadModelEnvelope<TReadModel>.Empty(id) : ReadModelEnvelope<TReadModel>.With(id, rm);
        }

        public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, CancellationToken cancellationToken)
        {
            foreach (var rmUpdate in readModelUpdates)
            {
                await _transientFaultHandler.TryAsync(
                    c => UpdateReadModelAsync(rmUpdate, updateReadModel, readModelContextFactory, c),
                    Label.Named("redis-readmodel-update"), cancellationToken).ConfigureAwait(false);
            }
        }

        private async Task UpdateReadModelAsync(ReadModelUpdate update,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, IReadModelContextFactory readModelContextFactory,
            CancellationToken token)
        {
            _logger.LogTrace("Updating read model {Id}", update.ReadModelId);

            var prefixedKey = new PrefixedKey(Constants.ReadModelPrefix, update.ReadModelId);

            var readModel = await _collection.FindByIdAsync(prefixedKey).ConfigureAwait(false);
            var readModelEnvelope = readModel is null
                ? ReadModelEnvelope<TReadModel>.Empty(update.ReadModelId)
                : ReadModelEnvelope<TReadModel>.With(update.ReadModelId, readModel);

            var context =
                readModelContextFactory.Create(readModelEnvelope.ReadModelId, readModelEnvelope.ReadModel is null);
            var updateResult = await updateReadModel(context, update.DomainEvents, readModelEnvelope, token);

            if (!updateResult.IsModified)
            {
                _logger.LogTrace("Read model not modified");
                return;
            }

            if (context.IsMarkedForDeletion)
            {
                _logger.LogTrace("Deleting read model because was marked for deletion");
                await DeleteAsync(update.ReadModelId, token).ConfigureAwait(false);

                return;
            }

            readModelEnvelope = updateResult.Envelope;
            var originalVersion = readModelEnvelope.ReadModel.Version;
            readModelEnvelope.ReadModel.Version = readModelEnvelope.Version;

            var hashEntries = _hashBuilder.BuildHashSet(readModelEnvelope.ReadModel).ToHashEntries();

            var tran = _multiplexer.GetDatabase().CreateTransaction();
            tran.AddCondition(Condition.HashEqual(prefixedKey, "Version", originalVersion));
            await tran.HashSetAsync(prefixedKey, hashEntries.ToArray());

            var result = await tran.ExecuteAsync().ConfigureAwait(false);
            if (!result)
            {
                _logger.LogTrace($"Transaction failed because of a wrong aggregate version, throwing {nameof(OptimisticConcurrencyException)}");

                throw new OptimisticConcurrencyException(
                    $"The version of the read model {prefixedKey.Key} is not the expected version {originalVersion}");
            }

            if (_logger.IsEnabled(LogLevel.Trace))
            {
                _logger.LogTrace("Updated and saved read model with id {Id}", readModelEnvelope.ReadModelId);
            }
        }
    }
}
