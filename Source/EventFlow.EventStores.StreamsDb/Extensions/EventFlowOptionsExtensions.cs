using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using StreamsDB.Driver;

namespace EventFlow.EventStores.StreamsDb.Extensions
{
	public static class EventFlowOptionsExtensions
	{
		public static IEventFlowOptions UseStreamsDbEventStore(
			this IEventFlowOptions eventFlowOptions,
			string connectionString)
		{
			StreamsDBClient client = null;

			using (var a = AsyncHelper.Wait)
			{
				a.Run(StreamsDBClient.Connect(connectionString), c => client = c);
			}

			return eventFlowOptions
				.RegisterServices(f => f.Register(r => client, Lifetime.Singleton))
				.UseEventStore<StreamsDbEventPersistence>();
		}
	}
}

