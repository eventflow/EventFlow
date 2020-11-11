using EventFlow.Aggregates.ExecutionResults;
using EventFlow.Commands;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Library.Template.Commands.Decrement
{
    public class Handler : CommandHandler<Entity, EntityId, IExecutionResult, Command>
    {
        public override async Task<IExecutionResult> ExecuteCommandAsync(Entity aggregate, Command command, CancellationToken cancellationToken)
        {
            var result = await aggregate.Decrement();

            return result;
        }
    }
}
