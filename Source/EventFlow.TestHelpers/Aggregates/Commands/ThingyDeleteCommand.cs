using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    [CommandVersion("ThingyDelete", 1)]
    public class ThingyDeleteCommand : Command<ThingyAggregate, ThingyId>
    {
        public PingId PingId { get; }

        public ThingyDeleteCommand(ThingyId aggregateId) : base(aggregateId)
        {
        }
    }

    public class ThingyDeleteCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyDeleteCommand>
    {
        public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyDeleteCommand command, CancellationToken cancellationToken)
        {
            aggregate.Delete();
            return Task.FromResult(0);
        }
    }
}
