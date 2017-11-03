using EventFlow.Tests.Exploration.StateMachine.Framework;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.States
{
    public class WaitingForCoinsState : IState
    {
        public WaitingForCoinsState()
        {
        }

        public WaitingForCoinsState(int value)
        {
            Value = value;
        }

        public int Value { get; }

        public IState Add(int value)
        {
            return new WaitingForCoinsState(value + Value);
        }
    }
}