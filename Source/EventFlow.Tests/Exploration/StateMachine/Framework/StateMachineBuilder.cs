using System;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Tests.Exploration.StateMachine.Framework.Transitions;

namespace EventFlow.Tests.Exploration.StateMachine.Framework
{
    public class StateMachineBuilder<TStateMachine, TIdentity> where TStateMachine : IAggregateRoot<TIdentity>
        where TIdentity : IIdentity
    {
        private StateMachineDefinition _definition;

        public IInSyntax StartWith<TState>() where TState : IState, new()
        {
            _definition = new StateMachineDefinition(typeof(TState));
            return new InBuilder(this);
        }

        private abstract class SyntaxBase
        {
            protected readonly StateMachineBuilder<TStateMachine, TIdentity> Root;

            protected SyntaxBase(StateMachineBuilder<TStateMachine, TIdentity> root)
            {
                Root = root;
            }
        }

        private class InBuilder : SyntaxBase, IInSyntax
        {
            public InBuilder(StateMachineBuilder<TStateMachine, TIdentity> root) : base(root)
            {
            }

            public IWhenSyntax<TState> In<TState>() where TState : IState
            {
                return new ContinueBuilder<TState>(Root);
            }
        }

        private class ContinueBuilder<TState> : SyntaxBase,
            IInBuildWhenSyntax<TState> where TState : IState
        {
            public ContinueBuilder(StateMachineBuilder<TStateMachine, TIdentity> root) : base(root)
            {
            }

            public IUseSyntax<TState, TSignal> When<TSignal>()
                where TSignal : IAggregateEvent<TStateMachine, TIdentity>
            {
                return new UseBuilder<TState, TSignal>(Root);
            }

            public IStateMachineDefinition Build()
            {
                return Root._definition;
            }

            public IWhenSyntax<T> In<T>() where T : IState
            {
                return new ContinueBuilder<T>(Root);
            }
        }

        private class UseBuilder<TState, TSignal> : SyntaxBase, IUseSyntax<TState, TSignal>
            where TState : IState
            where TSignal : IAggregateEvent<TStateMachine, TIdentity>
        {
            public UseBuilder(StateMachineBuilder<TStateMachine, TIdentity> root) : base(root)
            {
            }

            public IInBuildWhenSyntax<TState> Use<TTransition>()
                where TTransition : ITransition<TState, TSignal>, new()
            {
                Add(new GenericTransitionAdapter<TState, TSignal>(() => new TTransition()));
                return Continue();
            }

            public IInBuildWhenSyntax<TState> Use(Func<TState, TSignal, IState> transition)
            {
                Add(new FuncTransition<TState, TSignal>(transition));
                return Continue();
            }

            public IInBuildWhenSyntax<TState> Ignore()
            {
                Add(new IgnoreTransition());
                return Continue();
            }

            private void Add(ITransition transition)
            {
                Root._definition.Add<TState, TSignal>(transition);
            }

            private ContinueBuilder<TState> Continue()
            {
                return new ContinueBuilder<TState>(Root);
            }
        }

        public interface IInSyntax
        {
            IWhenSyntax<TState> In<TState>() where TState : IState;
        }

        public interface IInBuildWhenSyntax<TState> : IInSyntax, IWhenSyntax<TState>
            where TState : IState
        {
            IStateMachineDefinition Build();
        }

        public interface IWhenSyntax<TState> where TState : IState
        {
            IUseSyntax<TState, TSignal> When<TSignal>() where TSignal : IAggregateEvent<TStateMachine, TIdentity>;
        }

        public interface IUseSyntax<TState, out TSignal>
            where TState : IState
            where TSignal : IAggregateEvent<TStateMachine, TIdentity>
        {
            IInBuildWhenSyntax<TState> Use<TTransition>()
                where TTransition : ITransition<TState, TSignal>, new();

            IInBuildWhenSyntax<TState> Ignore();
        }
    }
}