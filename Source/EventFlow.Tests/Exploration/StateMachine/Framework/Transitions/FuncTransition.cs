using System;
using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Framework.Transitions
{
    internal class FuncTransition<TState, TSignal> : ITransition
    {
        private readonly Func<TState, TSignal, IState> _func;

        public FuncTransition(Func<TState, TSignal, IState> func)
        {
            _func = func;
        }

        public IState Execute(IState currentState, IAggregateEvent signal)
        {
            return _func((TState) currentState, (TSignal) signal);
        }
    }
}