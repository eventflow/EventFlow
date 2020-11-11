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
