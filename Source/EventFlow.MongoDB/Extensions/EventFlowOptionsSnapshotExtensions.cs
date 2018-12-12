using EventFlow.Extensions;
using EventFlow.MongoDB.SnapshotStores;

namespace EventFlow.MongoDB.Extensions
{
    public static class EventFlowOptionsSnapshotExtensions
    {
        public static IEventFlowOptions UseMongoDbSnapshotStore(
            this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .UseSnapshotStore<MongoDbSnapshotPersistence>();
        }
    }
}
