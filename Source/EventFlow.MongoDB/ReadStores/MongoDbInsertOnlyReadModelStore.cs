using EventFlow.Aggregates;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using MongoDB.Bson.Serialization.IdGenerators;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.MongoDB.ValueObjects;

namespace EventFlow.MongoDB.ReadStores
{
    public class MongoDbInsertOnlyReadModelStore<TReadModel> : IMongoDbInsertOnlyReadModelStore<TReadModel>
        where TReadModel : class, IMongoDbInsertOnlyReadModel, new()
    {
        private readonly ILog _log;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IInsertOnlyReadModelDescriptionProvider _readModelDescriptionProvider;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;


        public MongoDbInsertOnlyReadModelStore(
            ILog log,
            IMongoDatabase mongoDatabase,
            IInsertOnlyReadModelDescriptionProvider readModelDescriptionProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
        {
            _log = log;
            _mongoDatabase = mongoDatabase;
            _readModelDescriptionProvider = readModelDescriptionProvider;
            _transientFaultHandler = transientFaultHandler;
        }

        public async Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Information(
                $"Deleting '{typeof(TReadModel).PrettyPrint()}' with id '{id}', from '{readModelDescription.RootCollectionName}'!");

            var collection = _mongoDatabase.GetCollection<TReadModel>(readModelDescription.RootCollectionName.Value);
            await collection.DeleteOneAsync(x => x.Id.ToString() == id, cancellationToken);
        }

        public async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Information(
                $"Deleting ALL '{typeof(TReadModel).PrettyPrint()}' by DROPPING COLLECTION '{readModelDescription.RootCollectionName}'!");

            await _mongoDatabase.DropCollectionAsync(readModelDescription.RootCollectionName.Value, cancellationToken);
        }

        public Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
        {
            return Task.FromResult(ReadModelEnvelope<TReadModel>.Empty(id));
        }

        public async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() =>
            {
                var readModelIds = readModelUpdates
                    .Select(u => u.ReadModelId)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();
                return
                    $"Updating read models of type '{typeof(TReadModel).PrettyPrint()}' with _ids '{string.Join(", ", readModelIds)}' in collection '{readModelDescription.RootCollectionName}'";
            });

            foreach (var readModelUpdate in readModelUpdates)
            {
                await _transientFaultHandler.TryAsync(
                        c => UpdateReadModelAsync(readModelDescription, readModelUpdate, readModelContextFactory,
                            updateReadModel, c),
                        Label.Named("mongodb-read-model-update"),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UpdateReadModelAsync(ReadModelDescription readModelDescription, ReadModelUpdate readModelUpdate,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel, CancellationToken cancellationToken)
        {
            var readModelContext = readModelContextFactory.Create(readModelUpdate.ReadModelId, true);
            var collection = _mongoDatabase.GetCollection<TReadModel>(readModelDescription.RootCollectionName.Value);
            var readModelEnvelope = ReadModelEnvelope<TReadModel>.Empty(readModelUpdate.ReadModelId);

            var modelUpdateResult =
                await updateReadModel(readModelContext, readModelUpdate.DomainEvents, readModelEnvelope,
                    cancellationToken).ConfigureAwait(false);
            
            readModelEnvelope = modelUpdateResult.Envelope;
            readModelEnvelope.ReadModel.Id =
                ObjectIdGenerator.Instance.GenerateId(collection, readModelEnvelope.ReadModel);
            await collection.InsertOneAsync(
                readModelEnvelope.ReadModel,
                new InsertOneOptions {BypassDocumentValidation = true},
                cancellationToken);
        }

    }
}
