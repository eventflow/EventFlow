namespace EventFlow.MongoDB.EventStore
{
    public interface IMongoDbEventPersistenceInitializer
    {
        void Initialize();
    }
}
