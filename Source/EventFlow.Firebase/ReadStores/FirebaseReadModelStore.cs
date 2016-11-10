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
using FireSharp.Response;

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

        public async Task DeleteAllAsync(
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Information($"Deleting ALL '{typeof(TReadModel).PrettyPrint()}' by DELETING NODE '{readModelDescription.RootNodeName}'!");

            await _firebaseClient.DeleteAsync(readModelDescription.RootNodeName.Value);
        }

        public async Task<ReadModelEnvelope<TReadModel>> GetAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var readModelDescription = _readModelDescriptionProvider.GetReadModelDescription<TReadModel>();

            _log.Verbose(() => $"Fetching read model '{typeof(TReadModel).PrettyPrint()}' with ID '{id}' from node '{readModelDescription.RootNodeName}'");

            var path = $"{readModelDescription.RootNodeName}/{id}";

            var response = await _firebaseClient.GetAsync(path);

            return ReadModelEnvelope<TReadModel>.With(id, response.ResultAs<TReadModel>());
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
                return $"Updating read models of type '{typeof(TReadModel).PrettyPrint()}' with IDs '{string.Join(", ", readModelIds)}' in node '{readModelDescription.RootNodeName}'";
            });

            foreach (var readModelUpdate in readModelUpdates)
            {
                var path = $"{readModelDescription.RootNodeName}/{readModelUpdate.ReadModelId}";

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

                // TODO: Not sure about doing two server calls in order to update the _version?

                SetResponse setResponse = await _firebaseClient.SetAsync(path, readModelEnvelope.ReadModel);
                var updateVersion = await _firebaseClient.UpdateAsync(path, new { _version = readModelEnvelope.Version });
            }
        }
    }
}
