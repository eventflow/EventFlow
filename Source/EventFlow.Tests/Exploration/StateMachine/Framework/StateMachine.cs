using System;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Tests.Exploration.StateMachine.Framework.Transitions;

namespace EventFlow.Tests.Exploration.StateMachine.Framework
{
    public abstract class StateMachine<TStateMachine, TIdentity> : AggregateRoot<TStateMachine, TIdentity>
        where TIdentity : IIdentity
        where TStateMachine : StateMachine<TStateMachine, TIdentity>
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly object DefinitionLock = new object();

        private static IStateMachineDefinition _definition;
        // ReSharper enable StaticMemberInGenericType

        protected StateMachine(TIdentity id) : base(id)
        {
            var definition = GetDefinition();
            CurrentState = (IState) Activator.CreateInstance(definition.InitialStateType);
        }

        protected IState CurrentState { get; private set; }

        protected override void Emit<TEvent>(TEvent aggregateEvent, IMetadata metadata = null)
        {
            InvokeTransition(aggregateEvent);
            base.Emit(aggregateEvent, metadata);
        }

        protected override void ApplyEvent(IAggregateEvent<TStateMachine, TIdentity> aggregateEvent)
        {
            CurrentState = InvokeTransition(aggregateEvent);
            Version++;
        }

        protected abstract IStateMachineDefinition Define(StateMachineBuilder<TStateMachine, TIdentity> builder);

        private IState InvokeTransition(IAggregateEvent aggregateEvent)
        {
            var definition = GetDefinition();
            var key = new TransitionKey(CurrentState.GetType(), aggregateEvent.GetType());

            var transition = definition.GetTransition(key);
            if (transition == null)
                throw new InvalidOperationException("Signal not valid in this state.");

            var nextState = transition.Execute(CurrentState, aggregateEvent);
            if (nextState == null)
                throw new InvalidOperationException("Transition did not result in a valid state.");

            return nextState;
        }

        private IStateMachineDefinition GetDefinition()
        {
            lock (DefinitionLock)
            {
                if (_definition != null)
                    return _definition;
            }

            var definition = Define(new StateMachineBuilder<TStateMachine, TIdentity>());

            lock (DefinitionLock)
            {
                if (_definition != null)
                    return _definition;

                _definition = definition;
                return _definition;
            }
        }
    }
}