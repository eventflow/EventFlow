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

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;
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

        [Test]
        public void BasicFlow()
        {
            // Arrange
            using (var resolver = EventFlowOptions.New
                .AddEvents(EventFlowTestHelpers.Assembly)
                .AddCommandHandlers(EventFlowTestHelpers.Assembly)
                .RegisterServices(f => f.Register<IPingReadModelLocator, PingReadModelLocator>())
                .UseResolverAggregateRootFactory()
                .AddAggregateRoots(EventFlowTestHelpers.Assembly)
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .AddMetadataProvider<AddMachineNameMetadataProvider>()
                .AddMetadataProvider<AddEventTypeMetadataProvider>()
                .UseInMemoryReadStoreFor<InMemoryThingyReadModel>()
                .UseInMemoryReadStoreFor<PingReadModel, IPingReadModelLocator>()
                .AddSubscribers(typeof(Subscriber))
                .CreateResolver())
            {
                var commandBus = resolver.Resolve<ICommandBus>();
                var eventStore = resolver.Resolve<IAggregateStore>();
                var queryProcessor = resolver.Resolve<IQueryProcessor>();
                var id = ThingyId.New;

                // Act
                commandBus.Publish(new ThingyDomainErrorAfterFirstCommand(id), CancellationToken.None);
                commandBus.Publish(new ThingyPingCommand(id, PingId.New), CancellationToken.None);
                commandBus.Publish(new ThingyPingCommand(id, PingId.New), CancellationToken.None);
                var testAggregate = eventStore.Load<ThingyAggregate, ThingyId>(id, CancellationToken.None);
                var testReadModelFromQuery1 = queryProcessor.Process(
                    new ReadModelByIdQuery<InMemoryThingyReadModel>(id.Value), CancellationToken.None);
                var testReadModelFromQuery2 = queryProcessor.Process(
                    new InMemoryQuery<InMemoryThingyReadModel>(rm => rm.DomainErrorAfterFirstReceived), CancellationToken.None);
                var pingReadModels = queryProcessor.Process(
                    new InMemoryQuery<PingReadModel>(m => true), CancellationToken.None);

                // Assert
                pingReadModels.Should().HaveCount(2);
                testAggregate.DomainErrorAfterFirstReceived.Should().BeTrue();
                testReadModelFromQuery1.DomainErrorAfterFirstReceived.Should().BeTrue();
                testReadModelFromQuery2.Should().NotBeNull();
            }
        }
    }
}
