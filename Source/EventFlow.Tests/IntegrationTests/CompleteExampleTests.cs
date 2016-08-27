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
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    public class CompleteExampleTests
    {
        [Test]
        public async Task Example()
        {
            // We wire up EventFlow with all of our classes. Instead of adding events,
            // commands, etc. explicitly, we could have used the the simpler
            // AddDefaults(Assembly) instead. See each of the referenced classes below
            using (var resolver = EventFlowOptions.New
                .AddEvents(typeof(SimpleEvent))
                .AddCommands(typeof(SimpleCommand))
                .AddCommandHandlers(typeof(SimpleCommandHandler))
                .UseInMemoryReadStoreFor<SimpleReadModel>()
                .CreateResolver())
            {
                // Create a new identity for our aggregate root
                var simpleId = SimpleId.New;

                // Resolve the command bus and use it to publish a command
                var commandBus = resolver.Resolve<ICommandBus>();
                await commandBus.PublishAsync(
                    new SimpleCommand(simpleId, 42), CancellationToken.None)
                    .ConfigureAwait(false);

                // Resolve the query handler and use the built-in query for fetching
                // read models by identity to get our read model representing the
                // state of our aggregate root
                var queryProcessor = resolver.Resolve<IQueryProcessor>();
                var simpleReadModel = await queryProcessor.ProcessAsync(
                    new ReadModelByIdQuery<SimpleReadModel>(simpleId), CancellationToken.None)
                    .ConfigureAwait(false);

                // Verify that the read model has the expected magic number
                simpleReadModel.MagicNumber.Should().Be(42);
            }
        }

        // Represents the aggregate identity (ID)
        public class SimpleId : Identity<SimpleId>
        {
            public SimpleId(string value) : base(value) { }
        }

        // The aggregate root
        public class SimpleAggrenate : AggregateRoot<SimpleAggrenate, SimpleId>,
            IEmit<SimpleEvent>
        {
            private int? _magicNumber;

            public SimpleAggrenate(SimpleId id) : base(id) { }

            // Method invoked by our command
            public void SetMagicNumer(int magicNumber)
            {
                if (_magicNumber.HasValue)
                    throw DomainError.With("Magic number already set");

                Emit(new SimpleEvent(magicNumber));
            }

            // We apply the event as part of the event sourcing system. EventFlow
            // provides several different methods for doing this, e.g. state objects,
            // the Apply method is merely the simplest
            public void Apply(SimpleEvent aggregateEvent)
            {
                _magicNumber = aggregateEvent.MagicNumber;
            }
        }

        // A basic event containing some information
        public class SimpleEvent : AggregateEvent<SimpleAggrenate, SimpleId>
        {
            public SimpleEvent(int magicNumber)
            {
                MagicNumber = magicNumber;
            }

            public int MagicNumber { get; }
        }

        // Command for update magic number
        public class SimpleCommand : Command<SimpleAggrenate, SimpleId>
        {
            public SimpleCommand(
                SimpleId aggregateId,
                int magicNumber)
                : base(aggregateId)
            {
                MagicNumber = magicNumber;
            }

            public int MagicNumber { get; }
        }

        // Command handler for our command
        public class SimpleCommandHandler : CommandHandler<SimpleAggrenate, SimpleId, SimpleCommand>
        {
            public override Task ExecuteAsync(
                SimpleAggrenate aggregate, 
                SimpleCommand command,
                CancellationToken cancellationToken)
            {
                aggregate.SetMagicNumer(command.MagicNumber);
                return Task.FromResult(0);
            }
        }

        // Read model for our aggregate
        public class SimpleReadModel : IReadModel,
            IAmReadModelFor<SimpleAggrenate, SimpleId, SimpleEvent>
        {
            public int MagicNumber { get; private set; }

            public void Apply(
                IReadModelContext context,
                IDomainEvent<SimpleAggrenate, SimpleId, SimpleEvent> domainEvent)
            {
                MagicNumber = domainEvent.AggregateEvent.MagicNumber;
            }
        }
    }
}