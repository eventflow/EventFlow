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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Extensions;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.IntegrationTests.Sagas
{
    [Category(Categories.Integration)]
    public class AlternativeSagaStoreTests
    {
        private IRootResolver _resolver;
        private ICommandBus _commandBus;
        private IAggregateStore _aggregateStore;
        private AlternativeSagaStoreTestClasses.InMemorySagaStore _sagaStore;

        [SetUp]
        public void SetUp()
        {
            _resolver = EventFlowOptions.New
                .AddSagas(typeof(AlternativeSagaStoreTestClasses.TestSaga))
                .AddCommandHandlers(
                    typeof(AlternativeSagaStoreTestClasses.SagaTestACommandHandler),
                    typeof(AlternativeSagaStoreTestClasses.SagaTestBCommandHandler),
                    typeof(AlternativeSagaStoreTestClasses.SagaTestCCommandHandler))
                .AddEvents(
                    typeof(AlternativeSagaStoreTestClasses.SagaTestEventA),
                    typeof(AlternativeSagaStoreTestClasses.SagaTestEventB),
                    typeof(AlternativeSagaStoreTestClasses.SagaTestEventC))
                .RegisterServices(sr =>
                {
                    sr.RegisterType(typeof(AlternativeSagaStoreTestClasses.TestSagaLocator));
                    sr.Register<ISagaStore, AlternativeSagaStoreTestClasses.InMemorySagaStore>(Lifetime.Singleton);
                })
                .CreateResolver(false);

            _commandBus = _resolver.Resolve<ICommandBus>();
            _aggregateStore = _resolver.Resolve<IAggregateStore>();
            _sagaStore = (AlternativeSagaStoreTestClasses.InMemorySagaStore) _resolver.Resolve<ISagaStore>();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
        }

        [Test]
        public async Task StartedByCorrectly()
        {
            // Arrange
            var aggregateId = AlternativeSagaStoreTestClasses.SagaTestAggregateId.New;

            // Act
            await _commandBus.PublishAsync(
                new AlternativeSagaStoreTestClasses.SagaTestACommand(aggregateId),
                CancellationToken.None);

            // Assert
            var testAggregate = await _aggregateStore.LoadAsync<AlternativeSagaStoreTestClasses.SagaTestAggregate, AlternativeSagaStoreTestClasses.SagaTestAggregateId>(
                aggregateId,
                CancellationToken.None);
            testAggregate.As.Should().Be(1);
            testAggregate.Bs.Should().Be(1);
            testAggregate.Cs.Should().Be(1);
        }

        [Test]
        public async Task NotStarted()
        {
            // Arrange
            var aggregateId = AlternativeSagaStoreTestClasses.SagaTestAggregateId.New;

            // Act
            await _commandBus.PublishAsync(
                new AlternativeSagaStoreTestClasses.SagaTestBCommand(aggregateId),
                CancellationToken.None);

            // Assert
            var testAggregate = await _aggregateStore.LoadAsync<AlternativeSagaStoreTestClasses.SagaTestAggregate, AlternativeSagaStoreTestClasses.SagaTestAggregateId>(
                aggregateId,
                CancellationToken.None);
            testAggregate.As.Should().Be(0);
            testAggregate.Bs.Should().Be(1);
            testAggregate.Cs.Should().Be(0);
        }

        [Test]
        public void SagaLocatorReturningNullDoesntThrow()
        {
            // Arrange
            var aggregateId = AlternativeSagaStoreTestClasses.SagaTestAggregateId.With(Guid.Empty);

            // Act and Assert
            Assert.DoesNotThrowAsync(async () => await _commandBus.PublishAsync(
                new AlternativeSagaStoreTestClasses.SagaTestBCommand(aggregateId),
                CancellationToken.None));
        }

        [Test]
        public async Task SagaLocatorReturningNullDoesntCallSagaStore()
        {
            // Arrange
            var aggregateId = AlternativeSagaStoreTestClasses.SagaTestAggregateId.With(Guid.Empty);

            // Act
            await _commandBus.PublishAsync(
                new AlternativeSagaStoreTestClasses.SagaTestBCommand(aggregateId),
                CancellationToken.None);
            
            // Assert
            _sagaStore.UpdateShouldNotHaveBeenCalled();
        }
    }
}