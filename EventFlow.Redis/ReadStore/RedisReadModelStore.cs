using System.Reflection.Metadata;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.ReadStores;
using Redis.OM;
using Redis.OM.Searching;
using StackExchange.Redis;

namespace EventFlow.Redis.ReadStore;

public class RedisReadModelStore<TReadModel> : IReadModelStore<TReadModel>
    where TReadModel : RedisReadModel
{
    private readonly IRedisCollection<TReadModel> _collection;
    private readonly RedisConnectionProvider _provider;
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly IRedisHashBuilder _hashBuilder;
    private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

    public RedisReadModelStore(RedisConnectionProvider redisConnectionProvider,
        ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler, IConnectionMultiplexer multiplexer, IRedisHashBuilder hashBuilder)
    {
        _provider = redisConnectionProvider;
        _transientFaultHandler = transientFaultHandler;
        _multiplexer = multiplexer;
        _hashBuilder = hashBuilder;
        _collection = redisConnectionProvider.RedisCollection<TReadModel>();
    }

    public async Task DeleteAsync(string id, CancellationToken cancellationToken)
    {
        var prefixedKey = new PrefixedKey(Constants.ReadModelPrefix, id);
        var first = await _collection.FindByIdAsync(prefixedKey);
        if (first is null)
            return;

        await _collection.DeleteAsync(first);
    }

    public Task DeleteAllAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = _provider.Connection.DropIndexAndAssociatedRecords(typeof(TReadModel));
            if (!result)
            {
                //TODO log
            }

            return Task.CompletedTask;
        }
        catch (Exception e)
        {
            //TODO log
            throw;
        }
    }

    public async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
    {
        var rm = await _collection.FindByIdAsync(id);
        return rm is null ? ReadModelEnvelope<TReadModel>.Empty(id) : ReadModelEnvelope<TReadModel>.With(id, rm);
    }

    public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
        IReadModelContextFactory readModelContextFactory,
        Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
            Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, CancellationToken cancellationToken)
    {
        foreach (var rmUpdate in readModelUpdates)
        {
           await _transientFaultHandler.TryAsync(c => UpdateReadModelAsync(rmUpdate, updateReadModel, readModelContextFactory, c),
                Label.Named("redis-readmodel-update"), cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task UpdateReadModelAsync(ReadModelUpdate update,
        Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
            Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, IReadModelContextFactory readModelContextFactory,
        CancellationToken token)
    {
        ReadModelEnvelope<TReadModel> readModelEnvelope;
        var prefixedKey = new PrefixedKey(Constants.ReadModelPrefix, update.ReadModelId);
        
        var readModel = await _collection.FindByIdAsync(prefixedKey);
        readModelEnvelope = readModel is null
            ? ReadModelEnvelope<TReadModel>.Empty(update.ReadModelId)
            : ReadModelEnvelope<TReadModel>.With(update.ReadModelId, readModel); 
            
        var context =
            readModelContextFactory.Create(readModelEnvelope.ReadModelId, readModelEnvelope.ReadModel is null);
        var updateResult = await updateReadModel(context, update.DomainEvents, readModelEnvelope, token);
        
        if (!updateResult.IsModified)
            return;

        if (context.IsMarkedForDeletion)
        {
            await DeleteAsync(update.ReadModelId, token);
            return;
        }

        readModelEnvelope = updateResult.Envelope;
        var originalVersion = readModelEnvelope.ReadModel.Version;
        readModelEnvelope.ReadModel.Version = readModelEnvelope.Version;
        
        var hashEntries = _hashBuilder.BuildHashSet(readModelEnvelope.ReadModel).ToHashEntries();
        
        var tran = _multiplexer.GetDatabase().CreateTransaction();
        tran.AddCondition(Condition.HashEqual(prefixedKey, "Version", originalVersion));
        tran.HashSetAsync(prefixedKey, hashEntries.ToArray());

        var result = await tran.ExecuteAsync();
        if (!result)
        {
            throw new OptimisticConcurrencyException(
                $"The version of the readmodel {prefixedKey.Key} is not the expected version {originalVersion}");
        }
    }
    
}