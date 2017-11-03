using EventFlow.Core;

namespace EventFlow.Tests.Exploration.StateMachine.Scenario
{
    public class VendingMachineId : Identity<VendingMachineId>
    {
        public VendingMachineId(string value) : base(value)
        {
        }
    }
}