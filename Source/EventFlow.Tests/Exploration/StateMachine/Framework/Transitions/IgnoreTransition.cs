using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Framework.Transitions
{
    internal class IgnoreTransition : ITransition
    {
        public IState Execute(IState currentState, IAggregateEvent signal)
        {
            return currentState;
        }
    }
}