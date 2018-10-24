using EventFlow.Extensions;
using EventFlow.MongoDB.EventStore;

namespace EventFlow.MongoDB.Extensions
{
    public static class EventFlowOptionsMongoDbEventStoreExtensions
    {
        public static IEventFlowOptions UseMongoDbEventStore(this IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions.UseEventStore<MongoDbEventPersistence>();
        }
    }
}