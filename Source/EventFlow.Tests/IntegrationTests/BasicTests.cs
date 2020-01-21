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
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.ReadStores.InMemory.Queries;
using EventFlow.Subscribers;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using EventFlow.Tests.IntegrationTests.ReadStores.ReadModels;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests
{
    [TestFixture]
    [Category(Categories.Integration)]
    public class BasicTests
    {
        public class Subscriber : ISubscribeSynchronousTo<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent>
        {
            public Task HandleAsync(IDomainEvent<ThingyAggregate, ThingyId, ThingyDomainErrorAfterFirstEvent> e, CancellationToken cancellationToken)
            {
                Console.WriteLine("Subscriber got ThingyDomainErrorAfterFirstEvent");
                return Task.FromResult(0);
            }
        }

        public class PingReadModel :
            IReadModel,
            IAmReadModelFor<ThingyAggregate, ThingyId, ThingyPingEvent>
        {
            public PingId Id { get; private set; }

            public Task ApplyAsync(
                IReadModelContext context,
                IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent,
                CancellationToken cancellationToken)
            {
                Id = domainEvent.AggregateEvent.PingId;
                return Task.CompletedTask;
            }
        }

        public interface IPingReadModelLocator : IReadModelLocator { }

        public class PingReadModelLocator : IPingReadModelLocator
        {
            public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
            {
                if (!(domainEvent is IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> pingEvent))
                {
                    yield break;
                }
                yield return pingEvent.AggregateEvent.PingId.Value;
            }
        }

        [Test]
        public async Task BasicFlow()
        {
            // Arrange
            using var serviceProvider = EventFlowTestHelpers.Setup()
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly)
                .RegisterServices(c =>
                {
                    c.AddTransient<IPingReadModelLocator, PingReadModelLocator>();
                    c.AddTransient<IScopedContext, ScopedContext>();
                })
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .AddMetadataProvider<AddMachineNameMetadataProvider>()
                .AddMetadataProvider<AddEventTypeMetadataProvider>()
                .UseInMemoryReadStoreFor<InMemoryThingyReadModel>()
                .UseInMemoryReadStoreFor<PingReadModel, IPingReadModelLocator>()
                .AddSubscribers(typeof(Subscriber))
                .Services.BuildServiceProvider(true);
            var commandBus = serviceProvider.GetRequiredService<ICommandBus>();
            var eventStore = serviceProvider.GetRequiredService<IAggregateStore>();
            var queryProcessor = serviceProvider.GetRequiredService<IQueryProcessor>();
            var id = ThingyId.New;

            // Act
            await commandBus.PublishAsync(new ThingyDomainErrorAfterFirstCommand(id), CancellationToken.None).ConfigureAwait(false);
            await commandBus.PublishAsync(new ThingyPingCommand(id, PingId.New), CancellationToken.None).ConfigureAwait(false);
            await commandBus.PublishAsync(new ThingyPingCommand(id, PingId.New), CancellationToken.None).ConfigureAwait(false);
            var testAggregate = await eventStore.LoadAsync<ThingyAggregate, ThingyId>(id, CancellationToken.None).ConfigureAwait(false);
            var testReadModelFromQuery1 = await queryProcessor.ProcessAsync(
                    new ReadModelByIdQuery<InMemoryThingyReadModel>(id.Value), CancellationToken.None)
                .ConfigureAwait(false);
            var testReadModelFromQuery2 = await queryProcessor.ProcessAsync(
                    new InMemoryQuery<InMemoryThingyReadModel>(rm => rm.DomainErrorAfterFirstReceived), CancellationToken.None)
                .ConfigureAwait(false);
            var pingReadModels = await queryProcessor.ProcessAsync(
                    new InMemoryQuery<PingReadModel>(m => true), CancellationToken.None)
                .ConfigureAwait(false);

            // Assert
            pingReadModels.Should().HaveCount(2);
            testAggregate.DomainErrorAfterFirstReceived.Should().BeTrue();
            testReadModelFromQuery1.DomainErrorAfterFirstReceived.Should().BeTrue();
            testReadModelFromQuery2.Should().NotBeNull();
        }
    }
}
