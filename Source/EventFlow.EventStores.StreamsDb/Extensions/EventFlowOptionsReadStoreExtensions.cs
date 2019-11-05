using EventFlow.Configuration;
using EventFlow.EventStores.StreamsDb.ReadStores;
using EventFlow.Queries;
using EventFlow.ReadStores;
using Microsoft.Extensions.Hosting;

namespace EventFlow.EventStores.StreamsDb.Extensions
{
    public static class EventFlowOptionsReadStoreExtensions
	{
		public static IEventFlowOptions UseStreamsDbReadModel<TReadModel, TReadModelLocator>(
			this IEventFlowOptions eventFlowOptions)
			where TReadModel : class, IReadModel
			where TReadModelLocator : IReadModelLocator
		{
			return eventFlowOptions
				.RegisterServices(RegisterStreamsDbReadStore<TReadModel>)
				.RegisterServices(f =>
				{
					f.Register<IHostedService, SubscriptionBasedMultipleAggregateReadStoreManager<IStreamsDbReadModelStore<TReadModel>, TReadModel, TReadModelLocator>>(Lifetime.Singleton);
					f.Register<IReadStoreManager, SubscriptionBasedMultipleAggregateReadStoreManager<IStreamsDbReadModelStore<TReadModel>, TReadModel, TReadModelLocator>>(Lifetime.Singleton);
					f.Register<IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>, ReadModelByIdQueryHandler<IStreamsDbReadModelStore<TReadModel>, TReadModel>>();
				});
		}

		public static IEventFlowOptions UseStreamsDbReadModel<TReadModel>(
			this IEventFlowOptions eventFlowOptions)
			where TReadModel : class, IReadModel
		{
            return UseStreamsDbReadModel<TReadModel, NullReadModelLocator>(eventFlowOptions);
        }

		private static void RegisterStreamsDbReadStore<TReadModel>(
			IServiceRegistration serviceRegistration)
			where TReadModel : class, IReadModel
		{
			serviceRegistration.Register<IStreamsDbReadModelStore<TReadModel>, StreamsDbReadModelStore<TReadModel>>();
			serviceRegistration.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IStreamsDbReadModelStore<TReadModel>>());
		}

	}
}
