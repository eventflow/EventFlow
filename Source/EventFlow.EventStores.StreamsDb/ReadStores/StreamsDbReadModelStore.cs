using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Newtonsoft.Json;
using StreamsDB.Driver;

namespace EventFlow.EventStores.StreamsDb
{
	public class StreamsDbReadModelStore<TReadModel> :
		ReadModelStore<TReadModel>,
		IStreamsDbReadModelStore<TReadModel>
		where TReadModel : class, IReadModel
	{
		private readonly StreamsDBClient _client;

		private readonly IReadModelFactory<TReadModel> _readModelFactory;

		public StreamsDbReadModelStore(StreamsDBClient client, IReadModelFactory<TReadModel> readModelFactory, ILog log) : base(log)
		{
			_client = client;
			_readModelFactory = readModelFactory;
		}

		public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
		{
			var readModelType = typeof(TReadModel);
			var stream = $"{readModelType.Name.ToLowerInvariant()}-{id}";

			var (lastMessage, found) = await _client.DB().ReadLastMessageFromStream(stream);

			if (!found)
			{
				Log.Verbose(() => $"Could not find any StreamsDb read model '{readModelType.PrettyPrint()}' with ID '{id}'");
				return ReadModelEnvelope<TReadModel>.Empty(id);
			}

			var json = Encoding.UTF8.GetString(lastMessage.Value);
			var readModel = JsonConvert.DeserializeObject<TReadModel>(json);

			var readModelVersion = lastMessage.Position;

			Log.Verbose(() => $"Found StreamsDb read model '{readModelType.PrettyPrint()}' with ID '{id}' and version '{readModelVersion}'");

			return ReadModelEnvelope<TReadModel>.With(id, readModel, readModelVersion);
		}

		public override async Task UpdateAsync(
			IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
			IReadModelContextFactory readModelContextFactory,
			Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
			CancellationToken cancellationToken)
		{
			var readModelType = typeof(TReadModel);

			Log.Verbose(() =>
			{
				var readModelIds = readModelUpdates
					.Select(u => u.ReadModelId)
					.Distinct()
					.OrderBy(i => i)
					.ToList();

				return $"Updating read models of type '{typeof(TReadModel).PrettyPrint()}' with ids '{string.Join(", ", readModelIds)}' in stream '{readModelType}-<id>'";
			});

			foreach (var readModelUpdate in readModelUpdates)
			{
				await UpdateReadModelAsync(readModelContextFactory, updateReadModel, cancellationToken, readModelUpdate);
			}
		}

		private async Task UpdateReadModelAsync(
			IReadModelContextFactory readModelContextFactory,
			Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
			CancellationToken cancellationToken,
			ReadModelUpdate readModelUpdate)
		{
			var readModelId = readModelUpdate.ReadModelId;
			var readModelEnvelope = await GetAsync(readModelId, cancellationToken).ConfigureAwait(false);
			var readModel = readModelEnvelope.ReadModel;
			var isNew = readModel == null;

			if (readModel == null)
			{
				readModel = await _readModelFactory.CreateAsync(readModelId, cancellationToken).ConfigureAwait(false);
				readModelEnvelope = ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, readModel);
			}

			var readModelContext = readModelContextFactory.Create(readModelId, isNew);

			var originalVersion = readModelEnvelope.Version;

			var readModelUpdateResult = await updateReadModel(
					readModelContext,
					readModelUpdate.DomainEvents,
					readModelEnvelope,
					cancellationToken)
				.ConfigureAwait(false);

			if (!readModelUpdateResult.IsModified)
			{
				return;
			}

			readModelEnvelope = readModelUpdateResult.Envelope;

			if (readModelContext.IsMarkedForDeletion)
			{
				await DeleteAsync(readModelId, cancellationToken).ConfigureAwait(false);
				return;
			}

			var readModelType = typeof(TReadModel);
			var stream = $"{readModelType.Name.ToLowerInvariant()}-{readModelId}";

			var messageInput = new MessageInput
			{
				ID = Guid.NewGuid().ToString(),
				Type = $"{readModelType.Name}.{readModelEnvelope.Version}",
				Value = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(readModelEnvelope.ReadModel))
			};

			try
			{
				if (readModelUpdate is CursorBasedReadModelUpdate cursorBasedReadModelUpdate)
				{
					var cursorsMessageInput = new MessageInput
					{
						ID = Guid.NewGuid().ToString(),
						Type = "cursors",
						Value = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(cursorBasedReadModelUpdate.Cursors))
					};

					// todo: use transaction 
					await _client.DB().AppendStream(cursorBasedReadModelUpdate.CursorsStream, cursorsMessageInput).ConfigureAwait(false);
				}

				await _client.DB().AppendStream(stream, ConcurrencyCheck.ExpectStreamVersion(originalVersion.GetValueOrDefault()), messageInput).ConfigureAwait(false);
			}
			catch (Exception e)
			{
				throw new OptimisticConcurrencyException($"Read model '{readModelEnvelope.ReadModelId}' updated by another", e);
			}

			Log.Verbose(() => $"Updated StreamsDB read model {typeof(TReadModel).PrettyPrint()} with ID '{readModelId}' to version '{readModelEnvelope.Version}'");
		}

		public override Task DeleteAsync(string id, CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}

		public override Task DeleteAllAsync(CancellationToken cancellationToken)
		{
			throw new NotImplementedException();
		}
	}
}
