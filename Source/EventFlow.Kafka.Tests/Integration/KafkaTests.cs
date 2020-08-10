using Confluent.Kafka;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Kafka.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NUnit.Framework;
using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Kafka.Tests.Integration
{
    [Category(Categories.Integration)]
    public class KafkaTests
    {
        [Test, Timeout(10000), Retry(3)]
        public async Task Scenario()
        {
            try
            {


                using (var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
                {
                    GroupId = "test-consumer-group",
                    BootstrapServers = "localhost:9092",
                    AutoOffsetReset = AutoOffsetReset.Earliest,
                }).Build())
                using (var resolver = BuildResolver())
                {
                    consumer.Subscribe("eventflow.domainevent.thingy.thingy-ping");
                    var commandBus = resolver.Resolve<ICommandBus>();
                    var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();

                    var pingId = PingId.New;
                    await commandBus.PublishAsync(new ThingyPingCommand(ThingyId.New, pingId), CancellationToken.None).ConfigureAwait(false);

                    var kafkaMessage = consumer.Consume(CancellationToken.None);

                    kafkaMessage.TopicPartition.Topic.Should().Be("eventflow.domainevent.thingy.thingy-ping");

                    var pingEvent = (DomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>)eventJsonSerializer.Deserialize(
                        kafkaMessage.Message.Value,
                        new Aggregates.Metadata(kafkaMessage.Message.Headers
                        .ToDictionary(
                            x => x.Key,
                            x => Encoding.UTF8.GetString(x.GetValueBytes()))));

                    pingEvent.AggregateEvent.PingId.Should().Be(pingId);
                }
            }
            catch (Exception e)
            {

                throw;
            }
        }

        private IRootResolver BuildResolver(Func<IEventFlowOptions, IEventFlowOptions> configure = null)
        {
            configure = configure ?? (e => e);

            return configure(EventFlowOptions.New
                .PublishToKafka(new ProducerConfig { BootstrapServers = "localhost:9092" })
                .AddDefaults(EventFlowTestHelpers.Assembly))
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .CreateResolver(false);
        }
    }
}
