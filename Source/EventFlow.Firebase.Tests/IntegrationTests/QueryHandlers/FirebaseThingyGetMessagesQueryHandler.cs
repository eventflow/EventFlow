using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using FireSharp.Interfaces;
using EventFlow.Firebase.ReadStores;
using EventFlow.Firebase.Tests.IntegrationTests.ReadModels;
using System.Collections.ObjectModel;

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

            var path = $"{readModelDescription.RootNodeName}";

            /*
             * We could create an index and then use orderBy and startAt to filter on the server
             * see: https://firebase.google.com/docs/database/security/indexing-data
             * and: https://firebase.google.com/docs/database/rest/retrieve-data#filtering-by-a-specified-child-key
             * 
             * e.g. var q = QueryBuilder.New().OrderBy("ThingyId").StartAt(query.ThingyId.Value);
             */

            var response = await _firebaseClient.GetAsync(path);

            var items = response.ResultAs<Dictionary<string, FirebaseThingyMessageReadModel>>().Where(p => p.Value.ThingyId == query.ThingyId.Value);

            var list = items.Select(s => s.Value.ToThingyMessage()).ToList();

            var values = new ReadOnlyCollection<ThingyMessage>(list);

            return values;
        }
    }
}