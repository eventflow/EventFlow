using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Commands
{
    public class InsertCoinHandler : CommandHandler<VendingMachine, VendingMachineId, InsertCoin>
    {
        public override Task ExecuteAsync(VendingMachine aggregate, InsertCoin command,
            CancellationToken cancellationToken)
        {
            aggregate.InsertCoin(command.Value);
            return Task.FromResult(1);
        }
    }
}