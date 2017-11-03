using EventFlow.Commands;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Commands
{
    public class Select : Command<VendingMachine, VendingMachineId>
    {
        public Select(VendingMachineId aggregateId, Selection selection) : base(aggregateId)
        {
            Selection = selection;
        }

        public Selection Selection { get; }
    }
}