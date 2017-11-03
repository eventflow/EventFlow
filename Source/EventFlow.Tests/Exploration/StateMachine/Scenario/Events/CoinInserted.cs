using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Events
{
    public class CoinInserted : AggregateEvent<VendingMachine, VendingMachineId>
    {
        public CoinInserted(int value)
        {
            Value = value;
        }

        public int Value { get; }
    }
}