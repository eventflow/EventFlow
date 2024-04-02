// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests
{
    public class ReadMeExamples
    {
        [Test]
        public async Task Example()
        {
            // We wire up EventFlow with all of our classes. Instead of adding events,
            // commands, etc. explicitly, we could have used the the simpler
            // AddDefaults(Assembly) instead.
            var serviceCollection = new ServiceCollection()
                .AddLogging()
                .AddEventFlow(o => o
                    .AddEvents(typeof(ExampleEvent))
                    .AddCommands(typeof(ExampleCommand))
                    .AddCommandHandlers(typeof(ExampleCommandHandler))
                    .UseInMemoryReadStoreFor<ExampleReadModel>());

            using (var serviceProvider = serviceCollection.BuildServiceProvider())
            {
                // Create a new identity for our aggregate root
                var exampleId = ExampleId.New;

                // Resolve the command bus and use it to publish a command
                var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
                await commandBus.PublishAsync(
                        new ExampleCommand(exampleId, 42), CancellationToken.None);

                // Resolve the query handler and use the built-in query for fetching
                // read models by identity to get our read model representing the
                // state of our aggregate root
                var queryProcessor = serviceProvider.GetRequiredService<IQueryProcessor>();
                var exampleReadModel = await queryProcessor.ProcessAsync(
                        new ReadModelByIdQuery<ExampleReadModel>(exampleId), CancellationToken.None);

                // Verify that the read model has the expected magic number
                exampleReadModel.MagicNumber.Should().Be(42);
            }
        }

        // The aggregate root
        public class ExampleAggregate : AggregateRoot<ExampleAggregate, ExampleId>,
            IEmit<ExampleEvent>
        {
            private int? _magicNumber;

            public ExampleAggregate(ExampleId id) : base(id) { }

            // Method invoked by our command
            public void SetMagicNumber(int magicNumber)
            {
                if (_magicNumber.HasValue)
                    throw DomainError.With("Magic number already set");

                Emit(new ExampleEvent(magicNumber));
            }

            // We apply the event as part of the event sourcing system. EventFlow
            // provides several different methods for doing this, e.g. state objects,
            // the Apply method is merely the simplest
            public void Apply(ExampleEvent aggregateEvent)
            {
                _magicNumber = aggregateEvent.MagicNumber;
            }
        }

        // Represents the aggregate identity (ID)
        public class ExampleId : Identity<ExampleId>
        {
            public ExampleId(string value) : base(value) { }
        }

        // A basic event containing some information
        public class ExampleEvent : AggregateEvent<ExampleAggregate, ExampleId>
        {
            public ExampleEvent(int magicNumber)
            {
                MagicNumber = magicNumber;
            }

            public int MagicNumber { get; }
        }

        // Command for update magic number
        public class ExampleCommand : Command<ExampleAggregate, ExampleId>
        {
            public ExampleCommand(
                ExampleId aggregateId,
                int magicNumber)
                : base(aggregateId)
            {
                MagicNumber = magicNumber;
            }

            public int MagicNumber { get; }
        }

        // Command handler for our command
        public class ExampleCommandHandler
            : CommandHandler<ExampleAggregate, ExampleId, ExampleCommand>
        {
            public override Task ExecuteAsync(
                ExampleAggregate aggregate,
                ExampleCommand command,
                CancellationToken cancellationToken)
            {
                aggregate.SetMagicNumber(command.MagicNumber);
                return Task.CompletedTask;
            }
        }

        // Read model for our aggregate
        public class ExampleReadModel : IReadModel,
            IAmReadModelFor<ExampleAggregate, ExampleId, ExampleEvent>
        {
            public int MagicNumber { get; private set; }

            public Task ApplyAsync(
                IReadModelContext context,
                IDomainEvent<ExampleAggregate, ExampleId, ExampleEvent> domainEvent,
                CancellationToken cancellationToken)
            {
                MagicNumber = domainEvent.AggregateEvent.MagicNumber;
                return Task.CompletedTask;
            }
        }
    }
}
