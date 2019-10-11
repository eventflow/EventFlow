using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.ReadStores;

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
				.UseReadStoreFor<IStreamsDbReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
		}

		public static IEventFlowOptions UseStreamsDbReadModel<TReadModel>(
			this IEventFlowOptions eventFlowOptions)
			where TReadModel : class, IReadModel
		{
			return eventFlowOptions
				.RegisterServices(RegisterStreamsDbReadStore<TReadModel>)
				.UseReadStoreFor<IStreamsDbReadModelStore<TReadModel>, TReadModel>();
		}

		public static IEventFlowOptions UseStreamsDbReadModelFor<TAggregate, TIdentity, TReadModel>(
			this IEventFlowOptions eventFlowOptions)
			where TAggregate : IAggregateRoot<TIdentity>
			where TIdentity : IIdentity
			where TReadModel : class, IReadModel
		{
			return eventFlowOptions
				.RegisterServices(RegisterStreamsDbReadStore<TReadModel>)
				.UseReadStoreFor<TAggregate, TIdentity, IStreamsDbReadModelStore<TReadModel>, TReadModel>();
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
