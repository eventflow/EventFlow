using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    public class ThingyAddMessageHistoryCommand : Command<ThingyAggregate, ThingyId>
    {
        public ThingyMessage[] ThingyMessages { get; }

        public ThingyAddMessageHistoryCommand(
            ThingyId aggregateId,
            IEnumerable<ThingyMessage> thingyMessages)
            : base(aggregateId)
        {
            ThingyMessages = thingyMessages.ToArray();
        }
    }

    public class ThingyAddMessageHistoryCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyAddMessageHistoryCommand>
    {
        public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyAddMessageHistoryCommand command, CancellationToken cancellationToken)
        {
            aggregate.AddMessageHistory(command.ThingyMessages);
            return Task.FromResult(0);
        }
    }
}