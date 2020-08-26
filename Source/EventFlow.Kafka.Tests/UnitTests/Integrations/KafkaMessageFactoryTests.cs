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

using AutoFixture;
using Confluent.Kafka;
using EventFlow.Aggregates;
using EventFlow.EventStores;
using EventFlow.Kafka.Integrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Events;
using FluentAssertions;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;

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
