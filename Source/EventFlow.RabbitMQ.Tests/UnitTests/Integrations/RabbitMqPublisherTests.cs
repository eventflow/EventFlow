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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.RabbitMQ.Integrations;
using EventFlow.TestHelpers;
using AutoFixture;
using NUnit.Framework;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace EventFlow.RabbitMQ.Tests.UnitTests.Integrations
{
    [Category(Categories.Unit)]
    public class RabbitMqPublisherTests : TestsFor<RabbitMqPublisher>
    {
        private IRabbitMqConnectionFactory _rabbitMqConnectionFactoryMock;
        private IRabbitMqConfiguration _rabbitMqConfigurationMock;
        private ILogger<TransientFaultHandler<IRabbitMqRetryStrategy>> _logMock;
        private IModel _modelMock;
        private IRabbitConnection _rabbitConnectionMock;

        [SetUp]
        public void SetUp()
        {
            _rabbitMqConnectionFactoryMock = InjectMock<IRabbitMqConnectionFactory>();
            _rabbitMqConfigurationMock = InjectMock<IRabbitMqConfiguration>();
            _logMock = InjectMock<ILogger<TransientFaultHandler<IRabbitMqRetryStrategy>>>();

            Fixture.Inject<ITransientFaultHandler<IRabbitMqRetryStrategy>>(new TransientFaultHandler<IRabbitMqRetryStrategy>(
                _logMock,
                new RabbitMqRetryStrategy()));

            var basicPropertiesMock = Substitute.For<IBasicProperties>();
            _modelMock = Substitute.For<IModel>();
            _rabbitConnectionMock = Substitute.For<IRabbitConnection>();

            _rabbitMqConnectionFactoryMock
                .CreateConnectionAsync(Arg.Any<Uri>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(_rabbitConnectionMock));
            _rabbitMqConfigurationMock
                .Uri
                .Returns(new Uri("amqp://localhost"));
            _modelMock
                .CreateBasicProperties()
                .Returns(basicPropertiesMock);
        }

        private void ArrangeWorkingConnection()
        {
            _rabbitConnectionMock
                .WithModelAsync(Arg.Any<Func<IModel, Task>>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(0))
                .AndDoes(c =>
                {
                    c.Arg<Func<IModel, Task>>()(_modelMock).Wait(c.Arg<CancellationToken>());
                });
        }

        private void ArrangeBrokenConnection<TException>()
            where TException : Exception, new()
        {
            _rabbitConnectionMock
                .WithModelAsync(Arg.Any<Func<IModel, Task>>(), Arg.Any<CancellationToken>())
                .Throws<TException>();
        }

        [Test]
        public async Task PublishIsCalled()
        {
            // Arrange
            ArrangeWorkingConnection();
            var rabbitMqMessages = Fixture.CreateMany<RabbitMqMessage>().ToList();

            // Act
            await Sut.PublishAsync(rabbitMqMessages, CancellationToken.None);

            // Assert
            _modelMock
                .Received(rabbitMqMessages.Count)
                .BasicPublish(Arg.Any<string>(), Arg.Any<string>(), false, Arg.Any<IBasicProperties>(), Arg.Any<ReadOnlyMemory<byte>>());

            _rabbitConnectionMock.Received(0).Dispose();
        }

        [Test]
        public void ConnectionIsDisposedOnException()
        {
            // Arrange
            ArrangeBrokenConnection<InvalidOperationException>();
            var rabbitMqMessages = Fixture.CreateMany<RabbitMqMessage>().ToList();

            // Act
            Assert.ThrowsAsync<InvalidOperationException>(
                async () => await Sut.PublishAsync(rabbitMqMessages, CancellationToken.None));

            // Assert
            _rabbitConnectionMock.Received(1).Dispose();
        }
    }
}