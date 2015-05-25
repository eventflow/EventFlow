// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public abstract class IntegrationTest<TIntegrationTestConfiguration> : Test
        where TIntegrationTestConfiguration : IntegrationTestConfiguration, new()
    {
        protected IRootResolver Resolver { get; private set; }
        protected IEventStore EventStore { get; private set; }
        protected ICommandBus CommandBus { get; private set; }
        protected IReadModelPopulator ReadModelPopulator { get; private set; }
        protected TIntegrationTestConfiguration Configuration { get; private set; }

        [SetUp]
        public void SetUpIntegrationTest()
        {
            Configuration = new TIntegrationTestConfiguration();

            var eventFlowOptions = EventFlowOptions.New
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly);

            Resolver = Configuration.CreateRootResolver(eventFlowOptions);
            EventStore = Resolver.Resolve<IEventStore>();
            CommandBus = Resolver.Resolve<ICommandBus>();
            ReadModelPopulator = Resolver.Resolve<IReadModelPopulator>();
        }

        [TearDown]
        public void TearDownIntegrationTest()
        {
            Configuration.TearDown();
            Resolver.Dispose();
        }

        protected async Task PublishPingCommandAsync(TestId testId, int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                await CommandBus.PublishAsync(new PingCommand(testId, PingId.New), CancellationToken.None).ConfigureAwait(false);
            }
        }
    }
}
