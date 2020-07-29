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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.RabbitMQ.Integrations;
using EventFlow.TestHelpers;
using AutoFixture;
using Moq;
using NUnit.Framework;
using RabbitMQ.Client;

namespace EventFlow.RabbitMQ.Tests.UnitTests.Integrations
{
    [Category(Categories.Unit)]
    public class RabbitMqPublisherTests : TestsFor<RabbitMqPublisher>
    {
        private Mock<IRabbitMqConnectionFactory> _rabbitMqConnectionFactoryMock;
        private Mock<IRabbitMqConfiguration> _rabbitMqConfigurationMock;
        private Mock<ILog> _logMock;
        private Mock<IModel> _modelMock;
        private Mock<IRabbitConnection> _rabbitConnectionMock;

        [SetUp]
        public void SetUp()
        {
            _rabbitMqConnectionFactoryMock = InjectMock<IRabbitMqConnectionFactory>();
            _rabbitMqConfigurationMock = InjectMock<IRabbitMqConfiguration>();
            _logMock = InjectMock<ILog>();

            Fixture.Inject<ITransientFaultHandler<IRabbitMqRetryStrategy>>(new TransientFaultHandler<IRabbitMqRetryStrategy>(
                _logMock.Object,
                new RabbitMqRetryStrategy()));

            var basicPropertiesMock = new Mock<IBasicProperties>();
            _modelMock = new Mock<IModel>();
            _rabbitConnectionMock = new Mock<IRabbitConnection>();

            _rabbitMqConnectionFactoryMock
                .Setup(f => f.CreateConnectionAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(_rabbitConnectionMock.Object));
            _rabbitMqConfigurationMock
                .Setup(c => c.Uri)
                .Returns(new Uri("amqp://localhost"));
            _modelMock
                .Setup(m => m.CreateBasicProperties())
                .Returns(basicPropertiesMock.Object);
        }

        private void ArrangeWorkingConnection()
        {
            _rabbitConnectionMock
                .Setup(c => c.WithModelAsync(It.IsAny<Func<IModel, Task>>(), It.IsAny<CancellationToken>()))
                .Callback<Func<IModel, Task>, CancellationToken>((a, c) =>
                    {
                        a(_modelMock.Object).Wait(c);
                    })
                .Returns(Task.FromResult(0));
        }

        private void ArrangeBrokenConnection<TException>()
            where TException : Exception, new()
        {
            _rabbitConnectionMock
                .Setup(c => c.WithModelAsync(It.IsAny<Func<IModel, Task>>(), It.IsAny<CancellationToken>()))
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
            _modelMock.Verify(
                m => m.BasicPublish(It.IsAny<string>(), It.IsAny<string>(), false, It.IsAny<IBasicProperties>(), It.IsAny<byte[]>()),
                Times.Exactly(rabbitMqMessages.Count));
            _rabbitConnectionMock.Verify(c => c.Dispose(), Times.Never);
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
            _rabbitConnectionMock.Verify(c => c.Dispose(), Times.Once);
        }
    }
}