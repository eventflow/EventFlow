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

            throw new System.NotImplementedException();

            //var getResponse = await _firebaseClient.GetAsync<FirebaseThingyReadModel>(
            //    query.ThingyId.Value,
            //    d => d
            //        .RequestConfiguration(c => c
            //            .CancellationToken(cancellationToken)
            //            .AllowedStatusCodes((int)HttpStatusCode.NotFound))
            //        .Index(readModelDescription.IndexName.Value))
            //    .ConfigureAwait(false);

            //return getResponse != null && getResponse.Found
            //    ? getResponse.Version
            //    : null as long?;
        }
    }
}