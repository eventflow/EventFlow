using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Queries;
using FireSharp.Interfaces;
using EventFlow.Firebase.ReadStores;
using EventFlow.Firebase.Tests.IntegrationTests.ReadModels;

namespace EventFlow.Firebase.Tests.IntegrationTests.QueryHandlers
{
    class FirebaseThingyGetVersionQueryHandler : IQueryHandler<ThingyGetVersionQuery, long?>
    {
        private readonly IFirebaseClient _firebaseClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public FirebaseThingyGetVersionQueryHandler(
            IFirebaseClient firebaseClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
        {
            _firebaseClient = firebaseClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public async Task<long?> ExecuteQueryAsync(ThingyGetVersionQuery query, CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<FirebaseThingyReadModel>();
            var nodeName = $"{readModelDescription.RootNodeName.Value}/{query.ThingyId.Value}";

            var getResponse = await _firebaseClient.GetAsync(nodeName);

            var dyn = getResponse.ResultAs<dynamic>();
            var version = dyn == null ? null : (long?)dyn._version;

            return version;
        }
    }
}