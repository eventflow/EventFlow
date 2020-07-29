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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using Newtonsoft.Json;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    [CommandVersion("ThingyMultiplePings", 1)]
    public class ThingyMultiplePingsCommand : Command<ThingyAggregate, ThingyId>
    {
        public IReadOnlyCollection<PingId> PingIds { get; }

        public ThingyMultiplePingsCommand(ThingyId aggregateId, IEnumerable<PingId> pingIds)
            : this(aggregateId, CommandId.New, pingIds)
        {
        }

        public ThingyMultiplePingsCommand(ThingyId aggregateId, ISourceId sourceId, IEnumerable<PingId> pingIds)
            : base (aggregateId, sourceId)
        {
            PingIds = pingIds.ToList();
        }

        [JsonConstructor]
        public ThingyMultiplePingsCommand(ThingyId aggregateId, SourceId sourceId, IEnumerable<PingId> pingIds)
            : base(aggregateId, sourceId)
        {
            PingIds = pingIds.ToList();
        }
    }

    public class ThingyMultiplePingsCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyMultiplePingsCommand>
    {
        public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyMultiplePingsCommand command, CancellationToken cancellationToken)
        {
            foreach (var pingId in command.PingIds)
            {
                aggregate.Ping(pingId);
            }
            return Task.FromResult(0);
        }
    }
}