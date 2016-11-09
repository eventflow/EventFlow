using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.Firebase.ReadStores;
using EventFlow.ReadStores;
using FireSharp;
using FireSharp.Config;
using FireSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventFlow.Firebase.Extensions
{
    public static class FirebaseOptionsExtensions
    {
        public static IEventFlowOptions ConfigureFirebase(
            this IEventFlowOptions eventFlowOptions,
            string authSecret,
            string basePath)
        {
            IFirebaseConfig config = new FirebaseConfig()
            {
                AuthSecret = authSecret,
                BasePath = basePath,
            };

            return eventFlowOptions
                .ConfigureFirebase(config);
        }

        public static IEventFlowOptions ConfigureFirebase(
            this IEventFlowOptions eventFlowOptions,
            IFirebaseConfig firebaseConfig)
        {
            var firebaseClient = new FirebaseClient(firebaseConfig);
            return eventFlowOptions.ConfigureFirebase(() => firebaseClient);
        }

        public static IEventFlowOptions ConfigureFirebase(
            this IEventFlowOptions eventFlowOptions,
            Func<IFirebaseClient> firebaseClientFactory)
        {
            return eventFlowOptions.RegisterServices(sr =>
            {
                sr.Register(f => firebaseClientFactory(), Lifetime.Singleton);
                sr.Register<IReadModelDescriptionProvider, ReadModelDescriptionProvider>(Lifetime.Singleton, true);
            });
        }

        public static IEventFlowOptions UseFirebaseReadModel<TReadModel>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel, new()
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IFirebaseReadModelStore<TReadModel>, FirebaseReadModelStore<TReadModel>>();
                    f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IFirebaseReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IFirebaseReadModelStore<TReadModel>, TReadModel>();
        }

        public static IEventFlowOptions UseFirebaseReadModel<TReadModel, TReadModelLocator>(
            this IEventFlowOptions eventFlowOptions)
            where TReadModel : class, IReadModel, new()
            where TReadModelLocator : IReadModelLocator
        {
            return eventFlowOptions
                .RegisterServices(f =>
                {
                    f.Register<IFirebaseReadModelStore<TReadModel>, FirebaseReadModelStore<TReadModel>>();
                    f.Register<IReadModelStore<TReadModel>>(r => r.Resolver.Resolve<IFirebaseReadModelStore<TReadModel>>());
                })
                .UseReadStoreFor<IFirebaseReadModelStore<TReadModel>, TReadModel, TReadModelLocator>();
        }
    }
}
