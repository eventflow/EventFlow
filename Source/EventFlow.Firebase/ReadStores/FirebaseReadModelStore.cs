using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.ReadStores;
using EventFlow.Logs;
using FireSharp.Interfaces;
using EventFlow.Extensions;

namespace EventFlow.Firebase.ReadStores
{
    public class FirebaseReadModelStore<TReadModel> : IFirebaseReadModelStore<TReadModel> 
        where TReadModel : class, IReadModel, new()
    {
        private readonly ILog _log;
        private readonly IFirebaseClient _firebaseClient;
        private readonly IReadModelDescriptionProvider _readModelDescriptionProvider;

        public FirebaseReadModelStore(
            ILog log,
            IFirebaseClient firebaseClient,
            IReadModelDescriptionProvider readModelDescriptionProvider)
        {
            _log = log;
            _firebaseClient = firebaseClient;
            _readModelDescriptionProvider = readModelDescriptionProvider;
        }

        public Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id, 
            CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<
                IReadModelContext, 
                IReadOnlyCollection<IDomainEvent>, 
                ReadModelEnvelope<TReadModel>, 
                CancellationToken, 
                Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() =>
            {
                var readModelIds = readModelUpdates
                    .Select(u => u.ReadModelId)
                    .Distinct()
                    .OrderBy(i => i)
                    .ToList();
                return $"Updating read models of type '{typeof(TReadModel).PrettyPrint()}' with IDs '{string.Join(", ", readModelIds)}' in node '{readModelDescription.NodeName}'";
            });

            foreach (var readModelUpdate in readModelUpdates)
            {
                var path = $"{readModelDescription.NodeName}/{readModelUpdate.ReadModelId}";

                var response = await _firebaseClient.GetAsync(path);

                var firebaseResult = response.ResultAs<TReadModel>();

                var readModelEnvelope = (response.StatusCode == System.Net.HttpStatusCode.OK)
                    ? ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, firebaseResult)
                    : ReadModelEnvelope<TReadModel>.Empty(readModelUpdate.ReadModelId);

                readModelEnvelope = await updateReadModel(
                    readModelContext,
                    readModelUpdate.DomainEvents,
                    readModelEnvelope,
                    cancellationToken).ConfigureAwait(false);

                var x = _firebaseClient.SetAsync(path, readModelEnvelope.ReadModel);

                //if (response.StatusCode == System.Net.HttpStatusCode.OK)
                //    _firebaseClient.Update(path, readModelEnvelope.ReadModel);
                //else
                //    _firebaseClient.Push(path, readModelEnvelope.ReadModel);
            }
        }
    }
}
