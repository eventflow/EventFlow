// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.ValueObjects;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    public class ThingyAddMessageAndPingCommand : Command<ThingyAggregate, ThingyId>
    {
        public ThingyMessage ThingyMessage { get; }
        public PingId PingId { get; }
        public bool PingFirst { get; }

        public ThingyAddMessageAndPingCommand(
            ThingyId aggregateId,
            ThingyMessage thingyMessage,
            PingId pingId,
            bool pingFirst)
            : base(aggregateId)
        {
            ThingyMessage = thingyMessage;
            PingId = pingId;
            PingFirst = pingFirst;
        }
    }

    public class ThingyAddMessageAndPingCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyAddMessageAndPingCommand>
    {
        public override Task ExecuteAsync(
            ThingyAggregate aggregate,
            ThingyAddMessageAndPingCommand command,
            CancellationToken cancellationToken)
        {
            if (command.PingFirst)
            {
                aggregate.Ping(command.PingId);
                aggregate.AddMessage(command.ThingyMessage);
            }
            else
            {
                aggregate.AddMessage(command.ThingyMessage);
                aggregate.Ping(command.PingId);
            }

            return Task.FromResult(0);
        }
    }
}
