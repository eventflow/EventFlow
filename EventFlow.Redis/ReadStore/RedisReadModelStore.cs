using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.ReadStores;
using Microsoft.Extensions.Logging;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

public class RedisReadModelStore<TReadModel> : IReadModelStore<TReadModel>
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
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Failed to delete readmodel with id {Id} because it was not found", id);
            return;
        }

        await _collection.DeleteAsync(first).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Deleted Readmodel {Id}", id);
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(TReadModel));
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace(
                !result
                    ? "Failed to delete index and records of readmodel {Name}"
                    : "Deleted index and records of readmodel {Name}", typeof(TReadModel));

        return Task.CompletedTask;
    }

    public async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
    {
        var rm = await _collection.FindByIdAsync(id).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Found readmodel with {Id}: {ReadModel}", id, rm);

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
        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Updating readmodel {Id}", update.ReadModelId);

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
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Readmodel not modified");
            return;
        }

        if (context.IsMarkedForDeletion)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace("Deleting deadmodel because was marked for deletion");
            await DeleteAsync(update.ReadModelId, token).ConfigureAwait(false);

            return;
        }

        readModelEnvelope = updateResult.Envelope;
        var originalVersion = readModelEnvelope.ReadModel.Version;
        readModelEnvelope.ReadModel.Version = readModelEnvelope.Version;

        var hashEntries = _hashBuilder.BuildHashSet(readModelEnvelope.ReadModel).ToHashEntries();

        var tran = _multiplexer.GetDatabase().CreateTransaction();
        tran.AddCondition(Condition.HashEqual(prefixedKey, "Version", originalVersion));
        tran.HashSetAsync(prefixedKey, hashEntries.ToArray());

        var result = await tran.ExecuteAsync().ConfigureAwait(false);
        if (!result)
        {
            if (_logger.IsEnabled(LogLevel.Trace))
                _logger.LogTrace(
                    "Transaction failed because of a wrong aggregate version, throwing OptimisticConcurrencyException");

            throw new OptimisticConcurrencyException(
                $"The version of the readmodel {prefixedKey.Key} is not the expected version {originalVersion}");
        }

        if (_logger.IsEnabled(LogLevel.Trace))
            _logger.LogTrace("Updated and saved readmodel with id {Id}", readModelEnvelope.ReadModelId);
    }
}