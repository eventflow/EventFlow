using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Firebase.Extensions;
using EventFlow.Firebase.ReadStores;
using EventFlow.Firebase.Tests.IntegrationTests.QueryHandlers;
using EventFlow.Firebase.Tests.IntegrationTests.ReadModels;
using EventFlow.Firebase.ValueObjects;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;
using RootNodeName = EventFlow.Firebase.ValueObjects.RootNodeName;

namespace EventFlow.Firebase.Tests.IntegrationTests
{
    [Category("firebase")]
    public class FirebaseReadModelStoreTests : TestSuiteForReadModelStore
    {
        static readonly string FIREBASE_DATABASE_URL = "https://YOUR_FIREBASE_PROJECT.firebaseio.com/";

        private string _rootNodeName;

        public class TestReadModelDescriptionProvider : IReadModelDescriptionProvider
        {
            private readonly string _rootNodeName;

            public TestReadModelDescriptionProvider(
                string nodeName)
            {
                _rootNodeName = nodeName;
            }

            public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IReadModel
            {
                return new ReadModelDescription(
                    new RootNodeName(_rootNodeName));
            }
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _rootNodeName = $"eventflow-test-{Guid.NewGuid().ToString("D")}";

            var testReadModelDescriptionProvider = new TestReadModelDescriptionProvider(_rootNodeName);

            var resolver = eventFlowOptions
                .RegisterServices(sr =>
                {
                    sr.RegisterType(typeof(ThingyMessageLocator));
                    sr.Register<IReadModelDescriptionProvider>(c => testReadModelDescriptionProvider);
                })
                .ConfigureFirebase(FIREBASE_DATABASE_URL)
                .UseFirebaseReadModel<FirebaseThingyReadModel>()
                .UseFirebaseReadModel<FirebaseThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(FirebaseThingyGetQueryHandler),
                    typeof(FirebaseThingyGetVersionQueryHandler),
                    typeof(FirebaseThingyGetMessagesQueryHandler))
                .CreateResolver();

            //_firebaseClient = resolver.Resolve<IFirebaseClient>();

            return resolver;
        }

        protected override Task PurgeTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PurgeAsync<FirebaseThingyReadModel>(CancellationToken.None);
        }

        protected override Task PopulateTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PopulateAsync<FirebaseThingyReadModel>(CancellationToken.None);
        }
    }
}