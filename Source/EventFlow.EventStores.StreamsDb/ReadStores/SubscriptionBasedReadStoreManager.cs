using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using StreamsDB.Driver;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace EventFlow.EventStores.StreamsDb
{
	public class SubscriptionBasedReadStoreManager<TReadModelStore, TReadModel, TReadModelLocator> : IReadStoreManager<TReadModel>, IHostedService
		where TReadModelStore : IReadModelStore<TReadModel>
		where TReadModel : class, IReadModel
		where TReadModelLocator : IReadModelLocator
	{
		// ReSharper disable StaticMemberInGenericType
		private static readonly Type StaticReadModelType = typeof(TReadModel);
		private static readonly ISet<Type> AggregateEventTypes;
		private static readonly ISet<Type> AggregateTypes;
		// ReSharper enable StaticMemberInGenericType

		private string _cursorsStream;
		private Dictionary<string, long> _cursors;

		protected ILog Log { get; }
		protected IResolver Resolver { get; }
		protected TReadModelStore ReadModelStore { get; }
		protected IReadModelDomainEventApplier ReadModelDomainEventApplier { get; }
		protected IReadModelFactory<TReadModel> ReadModelFactory { get; }
		public TReadModelLocator ReadModelLocator { get; }
		public IEventJsonSerializer EventJsonSerializer { get; }
		protected StreamsDBClient Client { get; }


		public Type ReadModelType => StaticReadModelType;

		static SubscriptionBasedReadStoreManager()
		{
			var iAmReadModelForInterfaceTypes = StaticReadModelType
				.GetTypeInfo()
				.GetInterfaces()
				.Where(IsReadModelFor)
				.ToList();

			if (!iAmReadModelForInterfaceTypes.Any())
			{
				throw new ArgumentException(
					$"Read model type '{StaticReadModelType.PrettyPrint()}' does not implement any '{typeof(IAmReadModelFor<,,>).PrettyPrint()}'");
			}

			AggregateEventTypes = new HashSet<Type>(iAmReadModelForInterfaceTypes.Select(i => i.GetTypeInfo().GetGenericArguments()[2]));
			if (AggregateEventTypes.Count != iAmReadModelForInterfaceTypes.Count)
			{
				throw new ArgumentException(
					$"Read model type '{StaticReadModelType.PrettyPrint()}' implements ambiguous '{typeof(IAmReadModelFor<,,>).PrettyPrint()}' interfaces");
			}

			AggregateTypes = new HashSet<Type>(iAmReadModelForInterfaceTypes.Select(i => i.GetTypeInfo().GetGenericArguments()[0]));
		}

		private static bool IsReadModelFor(Type i)
		{
			if (!i.GetTypeInfo().IsGenericType)
			{
				return false;
			}

			var typeDefinition = i.GetGenericTypeDefinition();
			return typeDefinition == typeof(IAmReadModelFor<,,>) ||
				   typeDefinition == typeof(IAmAsyncReadModelFor<,,>);
		}

		public SubscriptionBasedReadStoreManager(
			ILog log,
			IResolver resolver,
			TReadModelStore readModelStore,
			IReadModelDomainEventApplier readModelDomainEventApplier,
			IReadModelFactory<TReadModel> readModelFactory,
			TReadModelLocator readModelLocator,
			IEventJsonSerializer eventJsonSerializer,
			StreamsDBClient client)
		{
			Log = log;
			Resolver = resolver;
			ReadModelStore = readModelStore;
			ReadModelDomainEventApplier = readModelDomainEventApplier;
			ReadModelFactory = readModelFactory;
			ReadModelLocator = readModelLocator;
			EventJsonSerializer = eventJsonSerializer;
			Client = client;

			_cursorsStream = $"{typeof(TReadModel).Name.ToLowerInvariant()}-cursors";
		}

		public Task UpdateReadStoresAsync(
			IReadOnlyCollection<IDomainEvent> domainEvents,
			CancellationToken cancellationToken)
		{
			// never update the readmodel via this source. Only update the readmodel via the subscription
			return Task.CompletedTask;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await InitializeCursors();
			Subscribe();
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		private async Task InitializeCursors()
		{
			var (message, found) = await Client.DB().ReadLastMessageFromStream(_cursorsStream);

			if (found)
			{
				_cursors = JsonConvert.DeserializeObject<Dictionary<string, long>>(Encoding.UTF8.GetString(message.Value));
			}
			else
			{
				_cursors = new Dictionary<string, long>();

				foreach (var aggregateType in AggregateTypes)
				{
					var groupStream = GetGroupStream(aggregateType);
					_cursors[groupStream] = 0;
				}
			}
		}

		private void Subscribe()
		{
			var channel = Channel.CreateUnbounded<Message>();

			foreach (var aggregateType in AggregateTypes)
			{
				var groupStream = GetGroupStream(aggregateType);
				var cursor = _cursors[groupStream];

				Task.Run(async () =>
				{
					var subscription = Client.DB().SubscribeStream(groupStream, cursor + 1);

					do
					{
						await subscription.MoveNext().ConfigureAwait(false);
						await channel.Writer.WriteAsync(subscription.Current);
					}
					while (true);
				});
			}

			Task.Run(async () =>
			{
				while (await channel.Reader.WaitToReadAsync())
				{
					if (channel.Reader.TryRead(out var message))
					{
						var eventJson = Encoding.UTF8.GetString(message.Value);
						var metadataJson = Encoding.UTF8.GetString(message.Header);

						var domainEvent = EventJsonSerializer.Deserialize(eventJson, metadataJson);

						// update cursors
						_cursors[message.Stream] = message.Position;

						await InternalUpdateReadStoresAsync(new ReadOnlyCollection<IDomainEvent>(new List<IDomainEvent> { domainEvent }), CancellationToken.None);
					}
				}
			});
		}

		private string GetGroupStream(Type aggregateType)
		{
			var aggregateName = aggregateType.Name.Substring(0, aggregateType.Name.Length - "aggregate".Length).ToLowerInvariant();
			var groupStream = $"#{aggregateName}";
			return groupStream;
		}

		public async Task InternalUpdateReadStoresAsync(
		  IReadOnlyCollection<IDomainEvent> domainEvents,
		  CancellationToken cancellationToken)
		{
			var relevantDomainEvents = domainEvents
				.Where(e => AggregateEventTypes.Contains(e.EventType))
				.ToList();

			if (!relevantDomainEvents.Any())
			{
				Log.Verbose(() => string.Format(
					"None of these events was relevant for read model '{0}', skipping update: {1}",
					StaticReadModelType.PrettyPrint(),
					string.Join(", ", domainEvents.Select(e => e.EventType.PrettyPrint()))
					));
				return;
			}

			Log.Verbose(() => string.Format(
				"Updating read model '{0}' in store '{1}' with these events: {2}",
				typeof(TReadModel).PrettyPrint(),
				typeof(TReadModelStore).PrettyPrint(),
				string.Join(", ", relevantDomainEvents.Select(e => e.ToString()))));

			var contextFactory = new ReadModelContextFactory(Resolver);

			var readModelUpdates = BuildReadModelUpdates(relevantDomainEvents);

			if (!readModelUpdates.Any())
			{
				Log.Verbose(() => string.Format(
					"No read model updates after building for read model '{0}' in store '{1}' with these events: {2}",
					typeof(TReadModel).PrettyPrint(),
					typeof(TReadModelStore).PrettyPrint(),
					string.Join(", ", relevantDomainEvents.Select(e => e.ToString()))));
				return;
			}

			await ReadModelStore.UpdateAsync(
				readModelUpdates,
				contextFactory,
				UpdateAsync,
				cancellationToken)
				.ConfigureAwait(false);
		}

		protected IReadOnlyCollection<ReadModelUpdate> BuildReadModelUpdates(
			IReadOnlyCollection<IDomainEvent> domainEvents)
		{
			var readModelUpdates = (
			   from de in domainEvents
			   let readModelIds = ReadModelLocator.GetReadModelIds(de)
			   from rid in readModelIds
			   group de by rid into g
			   select new CursorBasedReadModelUpdate(g.Key, g.OrderBy(d => d.Timestamp).ThenBy(d => d.AggregateSequenceNumber).ToList(), _cursors, _cursorsStream)
			   ).ToList();
			return readModelUpdates;
		}

		protected async Task<ReadModelUpdateResult<TReadModel>> UpdateAsync(
		   IReadModelContext readModelContext,
		   IReadOnlyCollection<IDomainEvent> domainEvents,
		   ReadModelEnvelope<TReadModel> readModelEnvelope,
		   CancellationToken cancellationToken)
		{
			var readModel = readModelEnvelope.ReadModel;
			if (readModel == null)
			{
				readModel = await ReadModelFactory.CreateAsync(
					readModelEnvelope.ReadModelId,
					cancellationToken)
					.ConfigureAwait(false);
			}

			await ReadModelDomainEventApplier.UpdateReadModelAsync(
				readModel,
				domainEvents,
				readModelContext,
				cancellationToken)
				.ConfigureAwait(false);

			return readModelEnvelope.AsModifedResult(
			   readModel,
			   readModelEnvelope.Version.GetValueOrDefault() + 1 // the best we can do
			   );
		}
	}
}
