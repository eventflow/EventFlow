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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable ClassNeverInstantiated.Local

namespace EventFlow.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    public class MultipleAggregateReadStoreManagerTests : IntegrationTest
    {
        private const string ReadModelId = "the one";
        
        [Test]
        public async Task EventOrdering()
        {
            // Repopulating read models that span multiple aggregates should have their events
            // applied in order using events time stamps
            
            // Arrange
            var idA = IdA.New;
            var idB = IdB.New;
            var i = 0;
            await CommandBus.PublishAsync(new CommandA(idA, i++), CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            await CommandBus.PublishAsync(new CommandA(idA, i++), CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            await CommandBus.PublishAsync(new CommandB(idB, i++), CancellationToken.None);
            await Task.Delay(TimeSpan.FromMilliseconds(10));
            await CommandBus.PublishAsync(new CommandB(idB, i++), CancellationToken.None);
            await ReadModelPopulator.PurgeAsync(typeof(ReadModelAB), CancellationToken.None);

            // Act
            await ReadModelPopulator.PopulateAsync(typeof(ReadModelAB), CancellationToken.None);
            
            // Assert
            var readModelAb = await QueryProcessor.ProcessAsync(
                new ReadModelByIdQuery<ReadModelAB>(ReadModelId),
                CancellationToken.None);
            
            readModelAb.Indexes.Should().BeEquivalentTo(
                new []{0, 1, 2, 3},
                o => o.WithStrictOrdering());
        }
        
        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            return eventFlowOptions
                .AddCommands(new []{typeof(CommandA), typeof(CommandA)})
                .AddCommandHandlers(typeof(CommandHandlerA), typeof(CommandHandlerB))
                .AddEvents(typeof(EventA), typeof(EventB))
                .UseInMemoryReadStoreFor<ReadModelAB, ReadModelLocatorAB>()
                .RegisterServices(sr => sr.RegisterType(typeof(ReadModelLocatorAB)))
                .CreateResolver();
        }
        
        private class IdA : Identity<IdA>
        {
            public IdA(string value) : base(value) { }
        }

        private class IdB : Identity<IdB>
        {
            public IdB(string value) : base(value) { }
        }

        private class AggregateA : AggregateRoot<AggregateA, IdA>, IEmit<EventA>
        {
            public AggregateA(IdA id) : base(id) { }

            public void A(int index)
            {
                Emit(new EventA(index));
            }

            public void Apply(EventA aggregateEvent) { }
        }
        
        private class AggregateB : AggregateRoot<AggregateB, IdB>, IEmit<EventB>
        {
            public AggregateB(IdB id) : base(id) { }

            public void B(int index)
            {
                Emit(new EventB(index));
            }

            public void Apply(EventB aggregateEvent) { }
        }

        private class EventA : AggregateEvent<AggregateA, IdA>
        {
            public int Index { get; }

            public EventA(int index)
            {
                Index = index;
            }
        }

        private class EventB : AggregateEvent<AggregateB, IdB>
        {
            public int Index { get; }

            public EventB(int index)
            {
                Index = index;
            }
        }

        private class CommandA : Command<AggregateA, IdA>
        {
            public int Index { get; }
            
            public CommandA(IdA aggregateId, int index) : base(aggregateId)
            {
                Index = index;
            }
        }
        
        private class CommandB : Command<AggregateB, IdB>
        {
            public int Index { get; }
            
            public CommandB(IdB aggregateId, int index) : base(aggregateId)
            {
                Index = index;
            }
        }

        private class CommandHandlerA : CommandHandler<AggregateA, IdA, CommandA>
        {
            public override Task ExecuteAsync(AggregateA aggregate, CommandA command, CancellationToken cancellationToken)
            {
                aggregate.A(command.Index);
                return Task.FromResult(0);
            }
        }

        private class CommandHandlerB : CommandHandler<AggregateB, IdB, CommandB>
        {
            public override Task ExecuteAsync(AggregateB aggregate, CommandB command, CancellationToken cancellationToken)
            {
                aggregate.B(command.Index);
                return Task.FromResult(0);
            }
        }

        private class ReadModelLocatorAB : IReadModelLocator
        {
            public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
            {
                return new[] {ReadModelId};
            }
        }

        private class ReadModelAB : IReadModel,
            IAmReadModelFor<AggregateA, IdA, EventA>,
            IAmReadModelFor<AggregateB, IdB, EventB>
        {
            private readonly List<int> _indexes = new List<int>();
            public IEnumerable<int> Indexes => _indexes;
            
            public void Apply(IReadModelContext context, IDomainEvent<AggregateA, IdA, EventA> domainEvent)
            {
                _indexes.Add(domainEvent.AggregateEvent.Index);
            }

            public void Apply(IReadModelContext context, IDomainEvent<AggregateB, IdB, EventB> domainEvent)
            {
                _indexes.Add(domainEvent.AggregateEvent.Index);
            }
        }
    }
}