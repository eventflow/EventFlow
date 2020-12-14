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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.AspNetCore.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.AspNetCore.Tests.IntegrationTests.Site
{
    [Category(Categories.Integration)]
    public class ModelBindingTests : SiteTestsBase
    {
        [Test]
        public async Task PingWithModelBinding()
        {
            // Act
            await GetAsync("thingy/pingWithModelBinding?id=thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3")
                .ConfigureAwait(false);
            await GetAsync("thingy/pingWithModelBinding?id=thingy-d15b1562-11f2-4645-8b1a-f8b946b566d3")
                .ConfigureAwait(false);
        }

        [Test]
        public async Task ValidSingleValue()
        {
            // Act
            var result = await GetAsync("thingy/singlevalue/123").ConfigureAwait(false);
            result.Should().Be("123");
        }

        [Test]
        public void InvalidSingleValue()
        {
            // Arrange + Act
            Func<Task> call = async () => await GetAsync("thingy/singlevalue/asdf").ConfigureAwait(false);

            call.Should().Throw<HttpRequestException>();
        }

        [Test]
        public async Task EventsContainMetadata()
        {
            ThingyId id = ThingyId.New;
            // Arrange + Act
            await GetAsync($"thingy/ping?id={id}").ConfigureAwait(false);

            // Assert
            var events = await EventStore.LoadEventsAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None);
            IMetadata metadata = events.Single().Metadata;

            metadata.Should().Contain(new[]
                {
                    new KeyValuePair<string, string>("user_claim[sid]", "test-sid"),
                    new KeyValuePair<string, string>("user_claim[role]", "test-role-1;test-role-2")
                }
            );

            metadata.Should().NotContain(
                new KeyValuePair<string, string>("user_claim[name]", "test-name"));
        }

        protected override void ConfigureAspNetCore(AspNetCoreEventFlowOptions options)
        {
            options
                .RunBootstrapperOnHostStartup()
                .UseMvcJsonOptions()
                .UseModelBinding()
                .AddUserClaimsMetadata()
                .UseLogging();
        }
    }
}
