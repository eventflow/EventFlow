using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleCommandHandler
        : CommandHandler<ExampleAggrenate, ExampleId, ExampleCommand>
    {
        public override Task ExecuteAsync(
            ExampleAggrenate aggregate,
            ExampleCommand command,
            CancellationToken cancellationToken)
        {
            aggregate.SetMagicNumer(command.MagicNumber);
            return Task.FromResult(0);
        }
    }
}