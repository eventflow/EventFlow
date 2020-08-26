// The MIT License (MIT)
//
// Copyright (c) 2020 Rasmus Mikkelsen
// Copyright (c) 2020 eBay Software Foundation
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

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.TestHelpers;
using AutoFixture;
using Moq;
using NUnit.Framework;
using EventFlow.Kafka.Integrations;
using Confluent.Kafka;

namespace EventFlow.Kafka.Tests.UnitTests.Integrations
{
    [Category(Categories.Unit)]
    public class KafkaPublisherTests : TestsFor<KafkaPublisher>
    {
        private Mock<IKafkaProducerFactory> _kafkaProducerFactory;
        private ProducerConfig _kafkaConfiguration;
        private Mock<ILog> _logMock;
        private Mock<IProducer<string, string>> _producerMock;

        [SetUp]
        public void SetUp()
        {
            _kafkaProducerFactory = InjectMock<IKafkaProducerFactory>();
            _kafkaConfiguration = new ProducerConfig();
            Fixture.Inject(_kafkaConfiguration);
            _logMock = InjectMock<ILog>();

            Fixture.Inject<ITransientFaultHandler<IKafkaRetryStrategy>>(new TransientFaultHandler<IKafkaRetryStrategy>(
                _logMock.Object,
                new KafkaRetryStrategy()));

            _producerMock = new Mock<IProducer<string, string>>();

            _kafkaProducerFactory
                .Setup(f => f.CreateProducer())
                .Returns(_producerMock.Object);

        }

        private void ArrangeBrokenPublisher<TException>()
            where TException : Exception, new()
        {
            _producerMock
                .Setup(c => c.Produce(It.IsAny<TopicPartition>(), It.IsAny<Message<string, string>>(), null))
                .Throws<TException>();
        }

        private void ArrangeWorkingPublisher()
        {
            _producerMock
                .Setup(c => c.Produce(It.IsAny<TopicPartition>(), It.IsAny<Message<string, string>>(), null))
                .Verifiable();
        }


        [Test]
        public async Task PublishIsCalled()
        {
            // Arrange
            ArrangeWorkingPublisher();
            var kafkaMessages = Fixture.CreateMany<KafkaMessage>().ToList();

            // Act
            await Sut.PublishAsync(kafkaMessages, CancellationToken.None);

            // Assert
            _producerMock.Verify(m => m.Produce(It.IsAny<TopicPartition>(), It.IsAny<Message<string, string>>(), null),
                Times.Exactly(kafkaMessages.Count));

            _producerMock.Verify(c => c.Dispose(), Times.Never);
        }

        [Test]
        public void ConnectionIsDisposedOnException()
        {
            // Arrange
            ArrangeBrokenPublisher<ArgumentException>();
            var kafkaMessages = Fixture.CreateMany<KafkaMessage>().ToList();

            // Act
            Assert.ThrowsAsync<ArgumentException>(
                async () => await Sut.PublishAsync(kafkaMessages, CancellationToken.None));

            // Assert
            _producerMock.Verify(c => c.Dispose(), Times.Once);
        }


    }
}
