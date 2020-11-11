using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Console.Template.Domain.Commands.Increment
{
    public class Handler : CommandHandler<Entity, EntityId, IExecutionResult, Command>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(Entity aggregate, Command command, CancellationToken cancellationToken)
        {
            var result = await aggregate.Increment();

            return result;
        }
    }
}
