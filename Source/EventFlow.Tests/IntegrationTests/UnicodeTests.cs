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

using System;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Commands;
using EventFlow.Configuration;
using EventFlow.Configuration.EventNamingStrategy;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;

// ReSharper disable IdentifierTypo
// ReSharper disable StringLiteralTypo

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class UnicodeTests : Test
    {
        [Test]
        public void UpperCaseIdentityThrows()
        {
            // Arrange + Act
            Action action = () => new Identität1("Identität1-00000000-0000-0000-0000-000000000000");

            // Assert
            action.Should().Throw<ArgumentException>();
        }

        [Test]
        public void LowerCaseIdentityWorks()
        {
            // Arrange + Act
            var id = new Identität1("identität1-00000000-0000-0000-0000-000000000000");

            // Assert
            id.GetGuid().Should().BeEmpty();
        }

        [Test]
        public void UnicodeIdentities()
        {
            // Arrange + Act
            var identität = Identität1.New.Value;
            
            // Assert
            identität.Should().StartWith("identität1-");
        }

        [Test]
        public void UnicodeCommands()
        {
            // Arrange
            var commandDefinitions = new CommandDefinitionService(
                Mock<ILogger<CommandDefinitionService>>(),
                Mock<ILoadedVersionedTypes>());

            // Act
            Action action = () => commandDefinitions.Load(typeof(Cömmand));

            // Assert
            action.Should().NotThrow();
        }

        [Test]
        public void UnicodeEvents()
        {
            // Arrange
            var eventDefinitionService = new EventDefinitionService(
                Mock<ILogger<EventDefinitionService>>(),
                Mock<ILoadedVersionedTypes>(),
                new DefaultStrategy());

            // Act
            Action action = () => eventDefinitionService.Load(typeof(Püng1Event));

            // Assert
            action.Should().NotThrow();
        }

        [Test]
        public async Task UnicodeIntegration()
        {
            var resolver = EventFlowOptions.New()
                .AddEvents(typeof(Püng1Event))
                .AddCommands(typeof(Cömmand))
                .AddCommandHandlers(typeof(CömmandHändler))
                .RegisterServices(s => s.AddScoped<IEventNamingStrategy, NamespaceAndClassNameStrategy>())
                .ServiceCollection.BuildServiceProvider();

            var bus = resolver.GetRequiredService<ICommandBus>();
            await bus.PublishAsync(new Cömmand(), CancellationToken.None);
        }

        private class Identität1 : Identity<Identität1>
        {
            public Identität1(string value) : base(value)
            {
            }
        }

        private class Püng1Event : AggregateEvent<Aggregät, Identität1>
        {
        }

        private class Aggregät : AggregateRoot<Aggregät, Identität1>
        {
            public Aggregät(Identität1 id) : base(id)
            {
            }

            public void Püng()
            {
                this.Emit(new Püng1Event());
            }

            public void Apply(Püng1Event e) { }
        }

        private class Cömmand : Command<Aggregät, Identität1>
        {
            public Cömmand() : base(Identität1.New)
            {
            }
        }

        private class CömmandHändler : CommandHandler<Aggregät, Identität1, Cömmand>
        {
            public override Task ExecuteAsync(Aggregät aggregate, Cömmand command, CancellationToken cancellationToken)
            {
                aggregate.Püng();
                return Task.FromResult(true);
            }
        }
    }
}
