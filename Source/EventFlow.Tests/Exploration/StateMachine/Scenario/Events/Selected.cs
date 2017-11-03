using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Events
{
    public class Selected : AggregateEvent<VendingMachine, VendingMachineId>
    {
        public Selected(Selection selection, int price)
        {
            Selection = selection;
            Price = price;
        }

        public Selection Selection { get; }

        public int Price { get; }
    }
}