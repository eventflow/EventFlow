// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
// 

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.Documentation.GettingStarted
{
    [Category(Categories.Integration)]
    public class ExampleTests
    {
        [Test]
        public async Task GettingStartedExample()
        {
            // We wire up EventFlow with all of our classes. Instead of adding events,
            // commands, etc. explicitly, we could have used the the simpler
            // AddDefaults(Assembly) instead.
            using (var resolver = EventFlowOptions.New
                .AddEvents(typeof(ExampleEvent))
                .AddCommands(typeof(ExampleCommand))
                .AddCommandHandlers(typeof(ExampleCommandHandler))
                .UseInMemoryReadStoreFor<ExampleReadModel>()
                .CreateResolver())
            {
                // Create a new identity for our aggregate root
                var exampleId = ExampleId.New;

                // Define some important value
                const int magicNumber = 42;

                // Resolve the command bus and use it to publish a command
                var commandBus = resolver.Resolve<ICommandBus>();
                await commandBus.PublishAsync(
                    new ExampleCommand(exampleId, magicNumber),
                    CancellationToken.None)
                    .ConfigureAwait(false);

                // Resolve the query handler and use the built-in query for fetching
                // read models by identity to get our read model representing the
                // state of our aggregate root
                var queryProcessor = resolver.Resolve<IQueryProcessor>();
                var exampleReadModel = await queryProcessor.ProcessAsync(
                    new ReadModelByIdQuery<ExampleReadModel>(exampleId),
                    CancellationToken.None)
                    .ConfigureAwait(false);

                // Verify that the read model has the expected magic number
                exampleReadModel.MagicNumber.Should().Be(42);
            }
        }
    }
}