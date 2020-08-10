using AutoFixture;
using Confluent.Kafka;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Kafka.Integrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Extensions;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EventFlow.Kafka.Tests.UnitTests.Integrations
{
    [Category(Categories.Unit)]
    public class KafkaMessageFactoryTests : TestsFor<KafkaMessageFactory>
    {
        private ProducerConfig _kafkaConfiguration;
        private Mock<IEventJsonSerializer> _serializerMock;
        [SetUp]
        public void SetUp()
        {
            _kafkaConfiguration = new ProducerConfig();
            _serializerMock = new Mock<IEventJsonSerializer>();
            _serializerMock.Setup(x => x.Serialize(It.IsAny<IAggregateEvent>(), It.IsAny<IEnumerable<KeyValuePair<string, string>>>()))
                .Returns(new SerializedEvent("", "", 0, Aggregates.Metadata.Empty));


            Fixture.Inject(_serializerMock);
            Fixture.Inject(_kafkaConfiguration);
        }

        public void UseCustomTopicFactory()
        {
            Fixture.Inject<Func<IDomainEvent, TopicPartition>>((IDomainEvent e) =>
            {
                return new TopicPartition(e.AggregateType.FullName, new Partition(e.AggregateSequenceNumber));
            });
        }

        public void UseDefaultTopicFactory()
        {
            Fixture.Inject<Func<IDomainEvent, TopicPartition>>(null);
        }


        [Test]
        public void DefaultTopicFactoryIsUsedWhenACustomIsNotProvided()
        {
            // Arrange
            UseDefaultTopicFactory();
            var metaData = new Aggregates.Metadata
            {
                AggregateName = "thingy",
                EventName = "thingyping"
            };
            var domainMessage = ADomainEvent<ThingyPingEvent>(initMetadata: metaData);

            // Act
            var kafkaMessage = Sut.CreateMessage(domainMessage);


            // Assert
            kafkaMessage.TopicPartition.Topic.Should().Be("eventflow.domainevent.thingy.thingyping");

            kafkaMessage.TopicPartition.Partition.Value.Should().Be(0);
        }

        [Test]
        public void CustomTopicFactoryIsUsedWhenProvided()
        {
            // Arrange
            UseCustomTopicFactory();
            var metaData = new Aggregates.Metadata
            {
                AggregateName = "thingy",
                EventName = "thingyping"
            };

            var domainMessage = ADomainEvent<ThingyPingEvent>(initMetadata: metaData);

            // Act
            var kafkaMessage = Sut.CreateMessage(domainMessage);


            // Assert
            kafkaMessage.TopicPartition.Topic.Should().Be("EventFlow.TestHelpers.Aggregates.ThingyAggregate");

            kafkaMessage.TopicPartition.Partition.Value.Should().Be(domainMessage.AggregateSequenceNumber);
        }


    }
}
