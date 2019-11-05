using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core.Caching;
using EventFlow.EventStores.StreamsDb.Integration;
using EventFlow.EventStores.StreamsDb.Integrations;
using EventFlow.Logs;
using Microsoft.Extensions.Hosting;
using StreamsDB.Driver;

namespace EventFlow.EventStores.StreamsDb.Extensions
{
    public static class EventFlowOptionsSubscriberExtensions
	{
		public static IEventFlowOptions UseStreamsDbSubscriber<TStreamsDbSubscriber>(this IEventFlowOptions eventFlowOptions, string stream, string subscriber)
			where TStreamsDbSubscriber: class, IStreamsDbSubscriber
		{
			return eventFlowOptions
				.RegisterServices(f =>
				{
					f.Register<IHostedService>(context => new StreamsDbSubscriberManager(
						context.Resolver.Resolve<ILog>(),
						context.Resolver.Resolve<IResolver>(),
						context.Resolver.Resolve<StreamsDBClient>(),
						stream,
                        subscriber
                    ), Lifetime.Singleton);

					f.Register<IStreamsDbSubscriber, TStreamsDbSubscriber>(Lifetime.Singleton);
				});
		}
	}
}
