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
using Assert = NUnit.Framework.Assert;

namespace EventFlow.Kafka.Tests.Integration
{
    [Category(Categories.Integration)]
    public class KafkaTests
    {

        private string _uri;

        [SetUp]
        public void SetUp()
        {
            _uri = Environment.GetEnvironmentVariable("KAFKA_URL");
            if (string.IsNullOrEmpty(_uri))
            {
                Assert.Inconclusive("The environment variable named 'KAFKA_URL' isn't set. Set it to e.g. 'localhost:9092'");
            }
        }

        [Test, Timeout(10000), Retry(3)]
        public async Task Scenario()
        {
            try
            {
                using (var consumer = new ConsumerBuilder<string, string>(new ConsumerConfig
                {
                    GroupId = Guid.NewGuid().ToString(),
                    BootstrapServers = _uri,
                    AutoOffsetReset = AutoOffsetReset.Earliest
                }).Build())
                using (var resolver = BuildResolver())
                {
                    
                    var commandBus = resolver.Resolve<ICommandBus>();
                    var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();

                    var pingId = PingId.New;
                    await commandBus.PublishAsync(new ThingyPingCommand(ThingyId.New, pingId), CancellationToken.None).ConfigureAwait(false);

                    consumer.Subscribe("eventflow.domainevent.thingy.thingy-ping");
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
                .PublishToKafka(new ProducerConfig { BootstrapServers = _uri })
                .AddDefaults(EventFlowTestHelpers.Assembly))
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .CreateResolver(false);
        }
    }
}
