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

using System.Threading;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests.UnitTests.Sagas
{
    public class SagaFlowTests
    {
        private IRootResolver _resolver;
        private ICommandBus _commandBus;
        private IEventStore _eventStore;

        [SetUp]
        public void SetUp()
        {
            _resolver = EventFlowOptions.New
                .AddAggregateRoots(
                    typeof (SagaTestClasses.SagaTestAggregate),
                    typeof (SagaTestClasses.TestSaga))
                .AddSagas(typeof (SagaTestClasses.TestSaga))
                .AddCommandHandlers(
                    typeof (SagaTestClasses.SagaTestACommandHandler),
                    typeof (SagaTestClasses.SagaTestBCommandHandler),
                    typeof (SagaTestClasses.SagaTestCCommandHandler))
                .AddEvents(
                    typeof (SagaTestClasses.SagaTestEventA),
                    typeof (SagaTestClasses.SagaTestEventB),
                    typeof (SagaTestClasses.SagaTestEventC),
                    typeof(SagaTestClasses.SagaEventA),
                    typeof(SagaTestClasses.SagaEventB),
                    typeof(SagaTestClasses.SagaEventC))
                .RegisterServices(sr =>
                    {
                        sr.RegisterType(typeof (SagaTestClasses.TestSagaLocator));
                    })
                .CreateResolver(false);

            _commandBus = _resolver.Resolve<ICommandBus>();
            _eventStore = _resolver.Resolve<IEventStore>();
        }

        [Test]
        public void StartedByCorrectly()
        {
            // Arrange
            var aggregateId = SagaTestClasses.SagaTestAggregateId.New;

            // Act
            _commandBus.Publish(new SagaTestClasses.SagaTestACommand(aggregateId), CancellationToken.None);

            // Assert
#pragma warning disable 618
            // TODO: Fix
            var testAggregate = _eventStore.LoadAggregate<SagaTestClasses.SagaTestAggregate, SagaTestClasses.SagaTestAggregateId>(aggregateId, CancellationToken.None);
#pragma warning restore 618
            testAggregate.As.Should().Be(1);
            testAggregate.Bs.Should().Be(1);
            testAggregate.Cs.Should().Be(1);
        }

        [Test]
        public void NotStarted()
        {
            // Arrange
            var aggregateId = SagaTestClasses.SagaTestAggregateId.New;

            // Act
            _commandBus.Publish(new SagaTestClasses.SagaTestBCommand(aggregateId), CancellationToken.None);

            // Assert
#pragma warning disable 618
            // TODO: Fix
            var testAggregate = _eventStore.LoadAggregate<SagaTestClasses.SagaTestAggregate, SagaTestClasses.SagaTestAggregateId>(aggregateId, CancellationToken.None);
#pragma warning restore 618
            testAggregate.As.Should().Be(0);
            testAggregate.Bs.Should().Be(1);
            testAggregate.Cs.Should().Be(0);
        }
    }
}
