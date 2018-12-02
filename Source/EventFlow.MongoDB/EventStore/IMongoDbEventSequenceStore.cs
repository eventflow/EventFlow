namespace EventFlow.MongoDB.EventStore
{
    public interface IMongoDbEventSequenceStore
    {
        long GetNextSequence(string name);
    }
}