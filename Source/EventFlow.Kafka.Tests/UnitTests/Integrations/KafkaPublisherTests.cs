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

namespace EventFlow.Kafka.Tests
{
    [Category(Categories.Unit)]
    public class KafkaPublisherTests : TestsFor<KafkaPublisher>
    {
        private Mock<IKafkaProducerFactory> _kafkaProducerFactory;
        private ProducerConfig _kafkaConfiguration;
        private Mock<ILog> _logMock;
        private Mock<IProducer<string, KafkaMessage>> _producerMock;

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

            _producerMock = new Mock<IProducer<string, KafkaMessage>>();

            _kafkaProducerFactory
                .Setup(f => f.CreateProducer())
                .Returns(_producerMock.Object);

        }

        private void ArrangeBrokenPublisher<TException>()
            where TException : Exception, new()
        {
            _producerMock
                .Setup(c => c.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, KafkaMessage>>(), It.IsAny<CancellationToken>()))
                .Throws<TException>();
        }


        [Test]
        public async Task PublishIsCalled()
        {
            // Arrange
            var kafkaMessages = Fixture.CreateMany<KafkaMessage>().ToList();

            // Act
            await Sut.PublishAsync(kafkaMessages, CancellationToken.None);

            // Assert
            _producerMock.Verify(m => m.ProduceAsync(It.IsAny<string>(), It.IsAny<Message<string, KafkaMessage>>(), It.IsAny<CancellationToken>()),
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
