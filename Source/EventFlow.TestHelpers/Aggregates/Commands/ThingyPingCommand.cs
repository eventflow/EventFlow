// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
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
using EventFlow.Core;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using Newtonsoft.Json;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    [CommandVersion("ThingyPing", 1)]
    public class ThingyPingCommand : Command<ThingyAggregate, ThingyId>
    {
        public PingId PingId { get; }

        public ThingyPingCommand(ThingyId aggregateId, PingId pingId)
            : this(aggregateId, CommandId.New, pingId)
        {
        }

        public ThingyPingCommand(ThingyId aggregateId, ISourceId sourceId, PingId pingId)
            : base (aggregateId, sourceId)
        {
            PingId = pingId;
        }

        [JsonConstructor]
        public ThingyPingCommand(ThingyId aggregateId, SourceId sourceId, PingId pingId)
            : base(aggregateId, sourceId)
        {
            PingId = pingId;
        }
    }

    public class ThingyPingCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyPingCommand>
    {
        public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyPingCommand command, CancellationToken cancellationToken)
        {
            aggregate.Ping(command.PingId);
            return Task.FromResult(0);
        }
    }
}