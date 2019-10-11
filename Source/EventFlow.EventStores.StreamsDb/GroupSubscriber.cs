using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Sagas;
using EventFlow.Subscribers;
using Microsoft.Extensions.Hosting;
using StreamsDB.Driver;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.EventStores.StreamsDb
{
	class GroupSubscriber : IHostedService
	{
		private readonly StreamsDBClient _client;
		private readonly IDispatchToEventSubscribers _dispatchToEventSubscribers;
		private readonly IDispatchToSagas _dispatchToSagas;
		private readonly IEventJsonSerializer _serializer;
		private readonly ILoadedVersionedTypes _loadedVersionedTypes;

		public GroupSubscriber(StreamsDBClient client, IDispatchToEventSubscribers dispatchToEventSubscribers, IDispatchToSagas dispatchToSagas, 
			IEventJsonSerializer serializer, ILoadedVersionedTypes loadedVersionedTypes)
		{
			_client = client;
			_dispatchToEventSubscribers = dispatchToEventSubscribers;
			_dispatchToSagas = dispatchToSagas;
			_serializer = serializer;
			_loadedVersionedTypes = loadedVersionedTypes;
		}

		public async Task StartAsync(CancellationToken cancellationToken)
		{
			var aggregates = new List<string>();

			var nameReplace = new Regex("Id$");

			foreach (var sagaType in _loadedVersionedTypes.Sagas)
			{
				var sagaDetails = SagaDetails.From(sagaType);

				foreach(var aggregateEventType in sagaDetails.AggregateEventTypes)
				{
					var aggregateEventInterfaceType = aggregateEventType
						.GetTypeInfo()
						.GetInterfaces()
						.SingleOrDefault(i => i.GetTypeInfo().IsGenericType && i.GetGenericTypeDefinition() == typeof(IAggregateEvent<,>));

					if (aggregateEventInterfaceType != null)
					{
						var identityTypeName = aggregateEventInterfaceType.GetGenericArguments()[1].Name;
						var aggregate = nameReplace.Replace(identityTypeName, string.Empty).ToLowerInvariant();
						aggregates.Add(aggregate);						
					}
				}
			}		

			foreach (var aggregate in aggregates)
			{
				await Task.Run(async () =>
				{
					var groupSubscription = _client.DB().SubscribeStream($"#{aggregate}", 0);

					while (await groupSubscription.MoveNext())
					{
						var message = groupSubscription.Current;
						var eventJson = Encoding.UTF8.GetString(message.Value);
						var metadataJson = Encoding.UTF8.GetString(message.Header);

						var domainEvent = _serializer.Deserialize(eventJson, metadataJson);

						try
						{
							await _dispatchToSagas.ProcessAsync(new List<IDomainEvent> { domainEvent }, cancellationToken);
						}
						catch (DuplicateOperationException)
						{
							// do nothing
						}						
					}
				});
			}
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;

		}
	}
}
