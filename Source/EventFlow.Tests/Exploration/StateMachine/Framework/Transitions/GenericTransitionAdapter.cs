using System;
using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Framework.Transitions
{
    public class GenericTransitionAdapter<TState, TSignal> : ITransition
    {
        private readonly Func<ITransition<TState, TSignal>> _factory;

        public GenericTransitionAdapter(Func<ITransition<TState, TSignal>> factory)
        {
            _factory = factory;
        }

        public IState Execute(IState currentState, IAggregateEvent signal)
        {
            var transition = _factory();
            return transition.Execute((TState) currentState, (TSignal) signal);
        }
    }
}