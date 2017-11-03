using EventFlow.Tests.Exploration.StateMachine.Framework;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Events;
using EventFlow.Tests.Exploration.StateMachine.Scenario.States;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Transitions;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario
{
    public class VendingMachine : StateMachine<VendingMachine, VendingMachineId>
    {
        public VendingMachine(VendingMachineId id) : base(id)
        {
        }

        protected override IStateMachineDefinition Define(StateMachineBuilder<VendingMachine, VendingMachineId> builder)
        {
            return builder
                .StartWith<WaitingForCoinsState>()

                .In<WaitingForCoinsState>()
                .When<CoinInserted>().Use<AddCoinTransition>()
                .When<Selected>().Use<CheckEnoughCoinsTransition>()

                .In<SelectedState>()
                .When<CoinInserted>().Ignore()
                .When<Selected>().Ignore()

                .Build();
        }

        public void InsertCoin(int value)
        {
            Emit(new CoinInserted(value));
        }

        public void Select(Selection selection, int price)
        {
            Emit(new Selected(selection, price));
        }
    }
}