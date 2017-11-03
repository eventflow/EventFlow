using EventFlow.Commands;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Commands
{
    public class InsertCoin : Command<VendingMachine, VendingMachineId>
    {
        public InsertCoin(VendingMachineId aggregateId, int value) : base(aggregateId)
        {
            Value = value;
        }

        public int Value { get; }
    }
}