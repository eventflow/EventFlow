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

using EventFlow;
using EventFlow.Commands;
using EventFlow.Extensions;
using EventFlow.Queries;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Console.Template
{
    class Program
    {
        private static async Task Main(string[] args)
        {
            using (var resolver = EventFlowOptions.New
                .RegisterModule<Domain.Module>()
                .UseInMemoryReadStoreFor<Domain.EntityReadModel>()
                .CreateResolver())
            {
                // Create a new identity for our aggregate root
                var id = Guid.NewGuid();
                var identity = new Domain.EntityId(id);

                // Resolve the command bus and use it to publish a command
                var commandBus = resolver.Resolve<ICommandBus>();
                var queryProcessor = resolver.Resolve<IQueryProcessor>();

                System.Console.WriteLine("Use '+' and '-' to increment/decrement value");

                while (true)
                {
                    var operation = System.Console.ReadKey();

                    var commands = operation.KeyChar switch
                    {
                        '+' => new Command<Domain.Entity, Domain.EntityId>[] { new Domain.Commands.Increment.Command(identity) },
                        '-' => new Command<Domain.Entity, Domain.EntityId>[] { new Domain.Commands.Decrement.Command(identity) },
                        _ => Array.Empty<Command<Domain.Entity, Domain.EntityId>>()
                    };

                    foreach (var command in commands)
                    {
                        await commandBus
                            .PublishAsync(command, CancellationToken.None)
                            .ConfigureAwait(false);
                    }

                    if (commands.Any())
                    {
                        var readModel = await queryProcessor
                            .ProcessAsync(new ReadModelByIdQuery<Domain.EntityReadModel>(identity), CancellationToken.None)
                            .ConfigureAwait(false);

                        System.Console.WriteLine($"Entity with the ID '{id}' has a value of: {readModel.Value}");
                    }
                }
            }
        }
    }
}
