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

using System.Threading.Tasks;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    public class EnvironmentTests
    {
        [Test]
        public async Task AggregatesDontMix()
        {
            using (var resolver1 = EventFlowOptions.New.AddDefaults(EventFlowTestHelpers.Assembly).CreateResolver(false))
            using (var resolver2 = EventFlowOptions.New.AddDefaults(EventFlowTestHelpers.Assembly).CreateResolver(false))
            {
                // Arrange
                var thingyId = ThingyId.New;
                var pingId = PingId.New;

                // Act
                await resolver1.Resolve<ICommandBus>().PublishAsync(
                    new ThingyPingCommand(thingyId, pingId))
                    .ConfigureAwait(false);

                // Assert
                var aggregate = await resolver2.Resolve<IEventStore>().LoadAggregateAsync<ThingyAggregate, ThingyId>(thingyId).ConfigureAwait(false);
                aggregate.IsNew.Should().BeTrue();
            }
        }
    }
}