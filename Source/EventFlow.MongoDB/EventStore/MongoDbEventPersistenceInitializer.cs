
using EventFlow.MongoDB.ValueObjects;
using MongoDB.Driver;

namespace EventFlow.MongoDB.EventStore
{
    class MongoDbEventPersistenceInitializer : IMongoDbEventPersistenceInitializer
    {
        private IMongoDatabase _mongoDatabase;

        public MongoDbEventPersistenceInitializer(IMongoDatabase mongoDatabase)
        {
            _mongoDatabase = mongoDatabase;
        }
        public void Initialize()
        {
            var events = _mongoDatabase.GetCollection<MongoDbEventDataModel>(MongoDbEventPersistence.CollectionName);
            IndexKeysDefinition<MongoDbEventDataModel> keys =
                Builders<MongoDbEventDataModel>.IndexKeys.Ascending("AggregateId")
                    .Ascending("AggregateSequenceNumber");
            events.Indexes.CreateOne(
                new CreateIndexModel<MongoDbEventDataModel>(keys, new CreateIndexOptions { Unique = true }));
        }
    }
}
