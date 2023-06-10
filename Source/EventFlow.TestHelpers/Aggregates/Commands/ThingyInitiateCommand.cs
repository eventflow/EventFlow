// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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

using EventFlow.Commands;
using EventFlow.Core;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json.Serialization;

namespace EventFlow.TestHelpers.Aggregates.Commands
{
    [CommandVersion("ThingyInitiate", 1)]
    public class ThingyInitiateCommand : Command<ThingyAggregate, ThingyId>, ICommandInitator
    {

        public ThingyInitiateCommand(ThingyId aggregateId)
            : this(aggregateId, CommandId.New)
        {   }

        public ThingyInitiateCommand(ThingyId aggregateId, ISourceId sourceId)
            : base(aggregateId, sourceId)
        {   }

        [JsonConstructor]
        public ThingyInitiateCommand(ThingyId aggregateId, SourceId sourceId)
            : base(aggregateId, sourceId)
        {   }
    }

    public class ThingyInitiateCommandHandler : CommandHandler<ThingyAggregate, ThingyId, ThingyInitiateCommand>
    {
        public override Task ExecuteAsync(ThingyAggregate aggregate, ThingyInitiateCommand command, CancellationToken cancellationToken)
        {
            aggregate.Intiate();
            return Task.FromResult(0);
        }
    }
}