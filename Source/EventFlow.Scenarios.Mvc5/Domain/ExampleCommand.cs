using EventFlow.Commands;

namespace EventFlow.Scenarios.Mvc5.Domain
{
    public class ExampleCommand : Command<ExampleAggrenate, ExampleId>
    {
        public ExampleCommand(
            ExampleId aggregateId,
            int magicNumber)
            : base(aggregateId)
        {
            MagicNumber = magicNumber;
        }

        public int MagicNumber { get; }
    }
}