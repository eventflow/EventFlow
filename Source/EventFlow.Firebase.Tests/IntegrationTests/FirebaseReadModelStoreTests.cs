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
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;
using NodeName = EventFlow.Firebase.ValueObjects.NodeName;
using FireSharp.Interfaces;

namespace EventFlow.Firebase.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class FirebaseReadModelStoreTests : TestSuiteForReadModelStore
    {
        private IFirebaseClient _firebaseClient;
        private string _nodeName;

        public class TestReadModelDescriptionProvider : IReadModelDescriptionProvider
        {
            private readonly string _indexName;

            public TestReadModelDescriptionProvider(
                string indexName)
            {
                _indexName = indexName;
            }

            public ReadModelDescription GetReadModelDescription<TReadModel>() where TReadModel : IReadModel
            {
                return new ReadModelDescription(
                    new NodeName(_indexName));
            }
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            try
            {
                _nodeName = $"eventflow-test-{Guid.NewGuid().ToString("D")}";

                var testReadModelDescriptionProvider = new TestReadModelDescriptionProvider(_nodeName);

                var resolver = eventFlowOptions
                    .RegisterServices(sr =>
                    {
                        sr.RegisterType(typeof(ThingyMessageLocator));
                        sr.Register<IReadModelDescriptionProvider>(c => testReadModelDescriptionProvider);
                    })
                    .ConfigureFirebase(@"https://event-flow-47b18.firebaseio.com/")
                    .UseFirebaseReadModel<FirebaseThingyReadModel>()
                    .UseFirebaseReadModel<FirebaseThingyMessageReadModel, ThingyMessageLocator>()
                    .AddQueryHandlers(
                        typeof(FirebaseThingyGetQueryHandler),
                        typeof(FirebaseThingyGetVersionQueryHandler),
                        typeof(FirebaseThingyGetMessagesQueryHandler))
                    .CreateResolver();

                _firebaseClient = resolver.Resolve<IFirebaseClient>();

                //_firebaseClient.CreateIndex(_nodeName);
                //_firebaseClient.Map<FirebaseThingyMessageReadModel>(d => d
                //    .Index(_nodeName)
                //    .AutoMap());

                //_firebaseInstance.WaitForGeenStateAsync().Wait(TimeSpan.FromMinutes(1));

                return resolver;
            }
            catch
            {
                //_firebaseInstance.DisposeSafe("Failed to dispose ES instance");
                throw;
            }
        }

        [Test]
        public void my_test()
        {
            Console.WriteLine();
            PopulateTestAggregateReadModelAsync().GetAwaiter().GetResult();
        }

        protected override Task PurgeTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PurgeAsync<FirebaseThingyReadModel>(CancellationToken.None);
        }

        protected override Task PopulateTestAggregateReadModelAsync()
        {
            return ReadModelPopulator.PopulateAsync<FirebaseThingyReadModel>(CancellationToken.None);
        }

        [TearDown]
        public void TearDown()
        {
            //try
            //{
            //    Console.WriteLine($"Deleting test index '{_nodeName}'");
            //    _firebaseClient.DeleteIndex(
            //        _nodeName,
            //        r => r.RequestConfiguration(c => c.AllowedStatusCodes((int)HttpStatusCode.NotFound)));
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}
        }
    }
}