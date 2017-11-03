using System;
using System.Collections.Generic;
using EventFlow.Tests.Exploration.StateMachine.Framework.Transitions;

namespace EventFlow.Tests.Exploration.StateMachine.Framework
{
    internal class StateMachineDefinition : IStateMachineDefinition
    {
        private readonly Dictionary<TransitionKey, ITransition> _transitions
            = new Dictionary<TransitionKey, ITransition>();

        public StateMachineDefinition(Type initialStateType)
        {
            InitialStateType = initialStateType;
        }

        public Type InitialStateType { get; }

        public ITransition GetTransition(TransitionKey key)
        {
            _transitions.TryGetValue(key, out var value);
            return value;
        }

        public void Add<TState, TSignal>(ITransition transition)
        {
            var key = new TransitionKey(typeof(TState), typeof(TSignal));
            _transitions.Add(key, transition);
        }
    }
}