using EventFlow.Aggregates;

namespace EventFlow.EventStores.StreamsDb.Integration
{
    public class IntegrationEvent
    {
		public string Data { get; }
		public IMetadata Metadata { get; }

		public IntegrationEvent(string data, IMetadata metadata)
		{
			Data = data;
			Metadata = metadata;
		}
    }
}
