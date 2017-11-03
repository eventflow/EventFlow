using System;
using EventFlow.Tests.Exploration.StateMachine.Framework.Transitions;

namespace EventFlow.Tests.Exploration.StateMachine.Framework
{
    public interface IStateMachineDefinition
    {
        Type InitialStateType { get; }

        ITransition GetTransition(TransitionKey key);
    }
}