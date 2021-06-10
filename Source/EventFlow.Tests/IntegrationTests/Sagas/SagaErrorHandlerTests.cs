// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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

using EventFlow.Configuration;
using EventFlow.Sagas;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Sagas;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Tests.IntegrationTests.Sagas
{
    [Category(Categories.Integration)]
    public class SagaErrorHandlerTests : IntegrationTest
    {
        private Mock<ISagaErrorHandler<ThingySaga>> _thingySagaErrorHandler;

        [Test]
        public async Task DefaultSagaErrorHandlerDoNotHandleException()
        {
            // Arrange
            var thingyId = A<ThingyId>();

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None)
                .ConfigureAwait(false);
            Func<Task> commandPublishAction = async () =>
            {
                await CommandBus.PublishAsync(new ThingyThrowExceptionInSagaCommand(thingyId), CancellationToken.None)
                    .ConfigureAwait(false);
            };
            
            // Assert
            commandPublishAction.Should().Throw<Exception>()
                .WithMessage("Exception thrown (as requested by ThingySagaExceptionRequestedEvent)");
        }

        [Test]
        public async Task SpecificSagaErrorHandlerHandleException()
        {
            // Arrange
            var thingyId = A<ThingyId>();
            var realThingySagaErrorHandler = Resolver.Resolve<ThingySagaErrorHandler>();
            _thingySagaErrorHandler.Setup(s => s.HandleAsync(It.IsAny<ISagaId>(), It.IsAny<SagaDetails>(),
                    It.IsAny<Exception>(), It.IsAny<CancellationToken>()))
                .Returns((ISagaId sagaId, SagaDetails sagaDetails, Exception exception,
                        CancellationToken cancellationToken) =>
                    realThingySagaErrorHandler.HandleAsync(sagaId, sagaDetails, exception, cancellationToken));

            // Act
            await CommandBus.PublishAsync(new ThingyRequestSagaStartCommand(thingyId), CancellationToken.None)
                .ConfigureAwait(false);
            Func<Task> commandPublishAction = async () =>
            {
                await CommandBus.PublishAsync(new ThingyThrowExceptionInSagaCommand(thingyId), CancellationToken.None)
                    .ConfigureAwait(false);
            };

            // Assert
            commandPublishAction.Should().NotThrow<Exception>();
        }

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _thingySagaErrorHandler = new Mock<ISagaErrorHandler<ThingySaga>>();

            return eventFlowOptions
                .RegisterServices(sr =>
                {
                    sr.Register(_ => _thingySagaErrorHandler.Object);
                    sr.RegisterType(typeof(ThingySagaErrorHandler));
                })
                .CreateResolver();
        }
    }
}
