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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores.Files;
using EventFlow.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class BackwardCompatibilityTests : Test
    {
        private readonly ThingyId _thingyId = ThingyId.With("thingy-1acea1eb-3e11-45c0-83c1-bc32e57ee8e7");
        private IServiceProvider _serviceProvider;
        private ICommandBus _commandBus;
        private IAggregateStore _aggregateStore;

        [SetUp]
        public void SetUp()
        {
            var codeBase = ReflectionHelper.GetCodeBase(GetType().Assembly);
            var filesEventStoreDirectory = Path.GetFullPath(Path.Combine(codeBase, "..", "..", "..", "TestData", "FilesEventStore"));

            _serviceProvider = EventFlowOptions.New()
                .UseFilesEventPersistence(FilesEventStoreConfiguration.Create(filesEventStoreDirectory))
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly)
                .RegisterServices(sr => sr.AddScoped<IScopedContext, ScopedContext>())
                .ServiceCollection.BuildServiceProvider();

            _commandBus = _serviceProvider.GetRequiredService<ICommandBus>();
            _aggregateStore = _serviceProvider.GetRequiredService<IAggregateStore>();
        }

        [Test]
        public async Task ValidateTestAggregate()
        {
            // Act
            var testAggregate = await _aggregateStore.LoadAsync<ThingyAggregate, ThingyId>(_thingyId, CancellationToken.None);

            // Assert
            testAggregate.Version.Should().Be(2);
            testAggregate.PingsReceived.Should().Contain(PingId.With("95433aa0-11f7-4128-bd5f-18e0ecc4d7c1"));
            testAggregate.PingsReceived.Should().Contain(PingId.With("2352d09b-4712-48cc-bb4f-5560d7c52558"));
        }

        [Test, Explicit]
        public async Task CreateEventHelper()
        {
            await _commandBus.PublishAsync(new ThingyPingCommand(_thingyId, PingId.New), CancellationToken.None);
        }
    }
}
