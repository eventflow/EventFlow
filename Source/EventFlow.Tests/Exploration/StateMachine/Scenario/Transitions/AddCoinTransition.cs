using EventFlow.Tests.Exploration.StateMachine.Framework;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Events;
using EventFlow.Tests.Exploration.StateMachine.Scenario.States;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Transitions
{
    public class AddCoinTransition : ITransition<WaitingForCoinsState, CoinInserted>
    {
        public IState Execute(WaitingForCoinsState currentState, CoinInserted signal)
        {
            return currentState.Add(signal.Value);
        }
    }
}