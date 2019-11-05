using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.StreamsDb.Integrations;
using EventFlow.EventStores.StreamsDb.ReadStores;
using EventFlow.Extensions;
using StreamsDB.Driver;

namespace EventFlow.EventStores.StreamsDb.Extensions
{
	public static class EventFlowOptionsExtensions
	{
		public static IEventFlowOptions UseStreamsDbEventStore(
			this IEventFlowOptions eventFlowOptions,
			string connectionString,
			string service)
		{
			StreamsDBClient client = null;

			using (var a = AsyncHelper.Wait)
			{
				a.Run(StreamsDBClient.Connect(connectionString), c => client = c);
			}

			return eventFlowOptions
				.RegisterServices(f =>
				{
					f.Register(r => client, Lifetime.Singleton);
                    f.Register<NullReadModelLocator, NullReadModelLocator>();

					// todo: move to own extension method
					//f.Register<IStreamsDbMessageFactory, StreamsDbMessageFactory>(Lifetime.Singleton);
					//f.Register<IStreamsDbPublisher, StreamsDbPublisher>(Lifetime.Singleton);
					//f.Register<ISubscribeSynchronousToAll, StreamsDbDomainEventPublisher>();

					// f.Register(rc => new StreamsDbServiceConfiguration(service), Lifetime.Singleton);
				})
				//.AddMetadataProvider<ServiceMetadataProvider>()
				.UseEventStore<StreamsDbEventPersistence>();
		}
	}
}

