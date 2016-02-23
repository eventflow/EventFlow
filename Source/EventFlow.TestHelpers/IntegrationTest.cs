// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
// https://github.com/rasmus/EventFlow
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
//

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public abstract class IntegrationTest: Test
    {
        protected IRootResolver Resolver { get; private set; }
        protected IEventStore EventStore { get; private set; }
        protected IQueryProcessor QueryProcessor { get; private set; }
        protected ICommandBus CommandBus { get; private set; }
        protected IReadModelPopulator ReadModelPopulator { get; private set; }

        [SetUp]
        public void SetUpIntegrationTest()
        {
            var eventFlowOptions = EventFlowOptions.New
                .AddDefaults(EventFlowTestHelpers.Assembly);

            Resolver = CreateRootResolver(Options(eventFlowOptions));

            EventStore = Resolver.Resolve<IEventStore>();
            CommandBus = Resolver.Resolve<ICommandBus>();
            QueryProcessor = Resolver.Resolve<IQueryProcessor>();
            ReadModelPopulator = Resolver.Resolve<IReadModelPopulator>();
        }

        [TearDown]
        public void TearDownIntegrationTest()
        {
            Resolver?.Dispose();
        }

        protected virtual IEventFlowOptions Options(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions;
        }

        protected abstract IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions);

        protected async Task PublishPingCommandAsync(ThingyId thingyId, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                await CommandBus.PublishAsync(new ThingyPingCommand(thingyId, PingId.New), CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
