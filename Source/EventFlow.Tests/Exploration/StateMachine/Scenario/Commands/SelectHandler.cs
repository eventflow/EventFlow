using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Commands
{
    public class SelectHandler : CommandHandler<VendingMachine, VendingMachineId, Select>
    {
        private static readonly Dictionary<Selection, int> Prices
            = new Dictionary<Selection, int>
            {
                {Selection.Chocolate, 5},
                {Selection.Ice, 6},
                {Selection.Nuts, 7}
            };

        public override Task ExecuteAsync(VendingMachine aggregate, Select command, CancellationToken cancellationToken)
        {
            var selection = command.Selection;
            var price = Prices[selection];
            aggregate.Select(selection, price);
            return Task.FromResult(1);
        }
    }
}