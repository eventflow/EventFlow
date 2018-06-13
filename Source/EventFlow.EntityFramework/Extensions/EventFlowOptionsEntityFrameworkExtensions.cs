using EventFlow.Configuration;
using EventFlow.EntityFramework.EventStores;
using EventFlow.EventStores;
using EventFlow.Extensions;

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

        public static IEventFlowOptions UseEntityFrameworkEventStore<TDbContextProvider>(this IEventFlowOptions eventFlowOptions)
            where TDbContextProvider : class, IDbContextProvider
        {
            return eventFlowOptions
                .UseEventStore<EntityFrameworkEventPersistence>()
                .RegisterServices(s =>
                {
                    var serviceType = typeof(IDbContextProvider<IEventPersistence>);
                    var implementation = typeof(SpecificDbContextProvider<IEventPersistence, TDbContextProvider>);
                    s.Register(serviceType, implementation);
                });
        }

        public static IEventFlowOptions AddDbContextProvider<TContextProvider>(
            this IEventFlowOptions eventFlowOptions,
            Lifetime lifetime = Lifetime.AlwaysUnique) 
            where TContextProvider : class, IDbContextProvider
        {
            return eventFlowOptions.RegisterServices(s => s.Register<TContextProvider, TContextProvider>(lifetime));
        }
    }
}