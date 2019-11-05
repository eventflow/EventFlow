using EventFlow.Aggregates.ExecutionResults;
using EventFlow.EventStores.StreamsDb.Integration;
using System.Threading.Tasks;

namespace EventFlow.EventStores.StreamsDb.Integrations
{
	public interface IStreamsDbSubscriber
	{
        Task<IExecutionResult> HandleAsync(IntegrationEvent integrationEvent);
	}	
}
