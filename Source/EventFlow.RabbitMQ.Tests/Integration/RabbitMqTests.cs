// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.RabbitMQ.Extensions;
using EventFlow.RabbitMQ.Integrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test;
using EventFlow.TestHelpers.Aggregates.Test.Commands;
using EventFlow.TestHelpers.Aggregates.Test.Events;
using EventFlow.TestHelpers.Aggregates.Test.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.RabbitMQ.Tests.Integration
{
    public class RabbitMqTests
    {
        private Uri _uri;

        [SetUp]
        public void SetUp()
        {
            var url = Environment.GetEnvironmentVariable("RABBITMQ_URL");
            if (string.IsNullOrEmpty(url))
            {
                Assert.Inconclusive("The environment variabel named 'RABBITMQ_URL' isn't set. Set it to e.g. 'amqp://localhost'");
            }

            _uri = new Uri(url);
        }

        [Test, Timeout(10000)]
        public void Scenario()
        {
            using (var consumer = new RabbitMqConsumer(_uri, "eventflow", new[] { "#" }))
            using (var resolver = BuildResolver())
            {
                var commandBus = resolver.Resolve<ICommandBus>();
                var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();

                var pingId = PingId.New;
                commandBus.Publish(new PingCommand(TestId.New, pingId), CancellationToken.None);

                var rabbitMqMessage = consumer.GetMessages().Single();
                rabbitMqMessage.Exchange.Value.Should().Be("eventflow");
                rabbitMqMessage.RoutingKey.Value.Should().Be("eventflow.domainevent.test.ping-event.1");

                var pingEvent = (IDomainEvent<TestAggregate, TestId, PingEvent>)eventJsonSerializer.Deserialize(
                    rabbitMqMessage.Message,
                    new Metadata(rabbitMqMessage.Headers));

                pingEvent.AggregateEvent.PingId.Should().Be(pingId);
            }
        }

        [Test, Timeout(20000)]
        public void PublisherPerformance()
        {
            var exchange = new Exchange("eventflow");
            var routingKey = new RoutingKey("performance");
            var exceptions = new ConcurrentBag<Exception>();
            const int threadCount = 100;
            const int messagesPrThread = 200;

            using (var consumer = new RabbitMqConsumer(_uri, "eventflow", new[] {"#"}))
            using (var resolver = BuildResolver(o => o.RegisterServices(sr => sr.Register<ILog, NullLog>())))
            {
                var rabbitMqPublisher = resolver.Resolve<IRabbitMqPublisher>();
                var threads = Enumerable.Range(0, threadCount)
                    .Select(_ =>
                        {
                            var thread = new Thread(o => SendMessages(rabbitMqPublisher, messagesPrThread, exchange, routingKey, exceptions));
                            thread.Start();
                            return thread;
                        })
                    .ToList();

                foreach (var thread in threads)
                {
                    thread.Join();
                }

                var rabbitMqMessages = consumer.GetMessages(threadCount * messagesPrThread);
                rabbitMqMessages.Should().HaveCount(threadCount*messagesPrThread);
                exceptions.Should().BeEmpty();
            }
        }

        private static void SendMessages(
            IRabbitMqPublisher rabbitMqPublisher,
            int count,
            Exchange exchange,
            RoutingKey routingKey,
            ConcurrentBag<Exception> exceptions)
        {
            var guid = Guid.NewGuid();

            try
            {
                for (var i = 0; i < count; i++)
                {
                    var rabbitMqMessage = new RabbitMqMessage(
                        $"{guid}-{i}",
                        new Metadata(),
                        exchange,
                        routingKey,
                        new MessageId(Guid.NewGuid().ToString("D")));
                    rabbitMqPublisher.PublishAsync(CancellationToken.None, rabbitMqMessage).Wait();
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        private IRootResolver BuildResolver(Func<EventFlowOptions, EventFlowOptions> configure = null)
        {
            configure = configure ?? (e => e);

            return configure(EventFlowOptions.New
                .PublishToRabbitMq(RabbitMqConfiguration.With(_uri))
                .AddDefaults(EventFlowTestHelpers.Assembly))
                .CreateResolver(false);
        }
    }
}
