using EventFlow.Tests.Exploration.StateMachine.Framework;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario.States
{
    public class SelectedState : IState
    {
        public SelectedState(Selection selection, int change)
        {
            Selection = selection;
            Change = change;
        }

        public Selection Selection { get; }

        public int Change { get; }
    }
}