using EventFlow.Tests.Exploration.StateMachine.Framework;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Events;
using EventFlow.Tests.Exploration.StateMachine.Scenario.States;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.Transitions
{
    public class CheckEnoughCoinsTransition : ITransition<WaitingForCoinsState, Selected>
    {
        public IState Execute(WaitingForCoinsState currentState, Selected signal)
        {
            var change = currentState.Value - signal.Price;
            if (change >= 0)
                return new SelectedState(signal.Selection, change);

            return currentState;
        }
    }
}