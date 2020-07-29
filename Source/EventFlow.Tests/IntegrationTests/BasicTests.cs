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
using EventFlow.Configuration;
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

            public void Apply(IReadModelContext context, IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent> domainEvent)
            {
                Id = domainEvent.AggregateEvent.PingId;
            }
        }

        public interface IPingReadModelLocator : IReadModelLocator { }

        public class PingReadModelLocator : IPingReadModelLocator
        {
            public IEnumerable<string> GetReadModelIds(IDomainEvent domainEvent)
            {
                var pingEvent = domainEvent as IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>;
                if (pingEvent == null)
                {
                    yield break;
                }
                yield return pingEvent.AggregateEvent.PingId.Value;
            }
        }

        [TestCaseSource(nameof(TestCases))]
        public async Task BasicFlow(IEventFlowOptions eventFlowOptions)
        {
            // Arrange
            using (var resolver = eventFlowOptions
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly)
                .RegisterServices(f => f.Register<IPingReadModelLocator, PingReadModelLocator>())
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .AddMetadataProvider<AddMachineNameMetadataProvider>()
                .AddMetadataProvider<AddEventTypeMetadataProvider>()
                .UseInMemoryReadStoreFor<InMemoryThingyReadModel>()
                .UseInMemoryReadStoreFor<PingReadModel, IPingReadModelLocator>()
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .AddSubscribers(typeof(Subscriber))
                .CreateResolver())
            {
                var commandBus = resolver.Resolve<ICommandBus>();
                var eventStore = resolver.Resolve<IAggregateStore>();
                var queryProcessor = resolver.Resolve<IQueryProcessor>();
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

        public static IEnumerable<IEventFlowOptions> TestCases()
        {
            yield return EventFlowOptions.New;
        }
    }
}