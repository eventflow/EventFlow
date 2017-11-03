using EventFlow.Extensions;
using EventFlow.Tests.Exploration.StateMachine.Scenario;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Commands;
using EventFlow.Tests.Exploration.StateMachine.Scenario.Events;
using NUnit.Framework;

namespace EventFlow.Tests.Exploration.StateMachine
{
    public class StateMachineExplorationTests
    {
        [Test]
        public void Test()
        {
            var resolver =
                EventFlowOptions.New
                    .AddEvents(typeof(CoinInserted), typeof(Selected))
                    .AddCommands(typeof(InsertCoin), typeof(Select))
                    .AddCommandHandlers(typeof(InsertCoinHandler), typeof(SelectHandler))
                    .CreateResolver();

            var bus = resolver.Resolve<ICommandBus>();
            var id = VendingMachineId.New;

            bus.Publish(new InsertCoin(id, 2));
            bus.Publish(new Select(id, Selection.Chocolate));
            bus.Publish(new InsertCoin(id, 5));
            bus.Publish(new Select(id, Selection.Ice));
        }
    }
}