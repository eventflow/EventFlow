using EventFlow.Aggregates;

namespace EventFlow.Tests.Exploration.StateMachine.Framework
{
    public interface ITransition
    {
        IState Execute(IState currentState, IAggregateEvent signal);
    }

    public interface ITransition<in TState, in TSignal>
    {
        IState Execute(TState currentState, TSignal signal);
    }
}