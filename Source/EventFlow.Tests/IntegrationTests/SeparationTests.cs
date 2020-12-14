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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class SeparationTests
    {
        [Test]
        public async Task AggregatesDontMix()
        {
            using (var resolver1 = SetupEventFlow())
            using (var resolver2 = SetupEventFlow())
            {
                // Arrange
                var thingyId = ThingyId.New;
                var pingId = PingId.New;

                // Act
                await resolver1.Resolve<ICommandBus>().PublishAsync(
                    new ThingyPingCommand(thingyId, pingId))
                    .ConfigureAwait(false);

                // Assert
                var aggregate = await resolver2.Resolve<IAggregateStore>().LoadAsync<ThingyAggregate, ThingyId>(
                    thingyId,
                    CancellationToken.None)
                    .ConfigureAwait(false);
                aggregate.IsNew.Should().BeTrue();
            }
        }

        private static IRootResolver SetupEventFlow(Func<IEventFlowOptions, IEventFlowOptions> configure = null)
        {
            var eventFlowOptions = EventFlowOptions.New
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .AddDefaults(EventFlowTestHelpers.Assembly);

            if (configure != null)
            {
                eventFlowOptions = configure(eventFlowOptions);
            }

            return eventFlowOptions.CreateResolver(false);
        }
    }
}