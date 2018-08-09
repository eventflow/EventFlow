using EventFlow.Configuration;
using EventFlow.EntityFramework.EventStores;
using EventFlow.EntityFramework.ReadStores;
using EventFlow.EntityFramework.SnapshotStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Extensions
{
    public static class EventFlowOptionsEntityFrameworkExtensions
    {
        public static IEventFlowOptions ConfigureEntityFramework(
            this IEventFlowOptions eventFlowOptions,
            IEntityFrameworkConfiguration configuration = null)
        {
            configuration = configuration ?? EntityFrameworkConfiguration.New;
            return eventFlowOptions.RegisterServices(configuration.Apply);
        }

        public static IEventFlowOptions UseEntityFrameworkEventStore<TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
        {
            return eventFlowOptions
                .UseEventStore<EntityFrameworkEventPersistence<TDbContext>>();
        }

        public static IEventFlowOptions UseEntityFrameworkSnapshotStore<TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
        {
            return eventFlowOptions
                .UseSnapshotStore<EntityFrameworkSnapshotPersistence<TDbContext>>();
        }

        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel>();
        }

        public static IEventFlowOptions UseEntityFrameworkReadModel<TReadModel, TDbContext, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TDbContext : DbContext
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IEntityFrameworkReadModelStore<TReadModel>,
                        EntityFrameworkReadModelStore<TReadModel, TDbContext>>();
                    f.Register<IReadModelStore<TReadModel>>(r =>
                        r.Resolver.Resolve<IEntityFrameworkReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IEntityFrameworkReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }

        public static IEventFlowOptions AddDbContextProvider<TDbContext, TContextProvider>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.Singleton)
            where TContextProvider : class, IDbContextProvider<TDbContext>
            where TDbContext : DbContext
        {
            return eventFlowOptions.RegisterServices(s =>
                s.Register<IDbContextProvider<TDbContext>, TContextProvider>(lifetime));
        }
    }
}
