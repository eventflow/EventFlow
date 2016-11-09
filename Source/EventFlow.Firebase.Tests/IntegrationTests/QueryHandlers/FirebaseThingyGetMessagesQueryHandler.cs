using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using FireSharp.Interfaces;
using EventFlow.Firebase.ReadStores;
using EventFlow.Firebase.Tests.IntegrationTests.ReadModels;

namespace EventFlow.Firebase.Tests.IntegrationTests.QueryHandlers
{
    class FirebaseThingyGetMessagesQueryHandler : IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>
    {
        private readonly IFirebaseClient _firebaseClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public FirebaseThingyGetMessagesQueryHandler(
            IFirebaseClient firebaseClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
        {
            _firebaseClient = firebaseClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public async Task<IReadOnlyCollection<ThingyMessage>> ExecuteQueryAsync(ThingyGetMessagesQuery query, CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<FirebaseThingyMessageReadModel>();
            var nodeName = readModelDescription.NodeName.Value;

            throw new System.NotImplementedException();

            // Never do this
            //await _firebaseClient.FlushAsync(
            //    nodeName,
            //    d => d
            //        .RequestConfiguration(c => c
            //            .CancellationToken(cancellationToken)
            //            .AllowedStatusCodes((int)HttpStatusCode.NotFound)))
            //    .ConfigureAwait(false);
            //await _firebaseClient.RefreshAsync(
            //    nodeName,
            //    d => d
            //        .RequestConfiguration(c => c
            //            .CancellationToken(cancellationToken)
            //            .AllowedStatusCodes((int)HttpStatusCode.NotFound)))
            //    .ConfigureAwait(false);

            //var searchResponse = await _firebaseClient.SearchAsync<FirebaseThingyMessageReadModel>(d => d
            //    .RequestConfiguration(c => c
            //        .CancellationToken(cancellationToken)
            //        .AllowedStatusCodes((int)HttpStatusCode.NotFound))
            //    .Index(nodeName)
            //    .Query(q => q.Term(m => m.ThingyId, query.ThingyId.Value)))
            //    .ConfigureAwait(false);

            //return searchResponse.Documents
            //    .Select(d => d.ToThingyMessage())
            //    .ToList();
        }
    }
}