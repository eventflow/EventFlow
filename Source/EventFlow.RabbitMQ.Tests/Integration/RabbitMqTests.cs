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
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Configuration;
using EventFlow.EventStores;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.RabbitMQ.Extensions;
using EventFlow.RabbitMQ.Integrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Commands;
using EventFlow.TestHelpers.Aggregates.Events;
using EventFlow.TestHelpers.Aggregates.Queries;
using EventFlow.TestHelpers.Aggregates.ValueObjects;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.RabbitMQ.Tests.Integration
{
    [Category(Categories.Integration)]
    public class RabbitMqTests
    {
        private Uri _uri;
        private CancellationTokenSource _timeout;

        [SetUp]
        public void SetUp()
        {
            var url = Environment.GetEnvironmentVariable("RABBITMQ_URL");
            if (string.IsNullOrEmpty(url))
            {
                Assert.Inconclusive("The environment variable named 'RABBITMQ_URL' isn't set. Set it to e.g. 'amqp://localhost'");
            }

            _uri = new Uri(url);
            _timeout = new CancellationTokenSource(TimeSpan.FromMinutes(1));
        }

        [TearDown]
        public void TearDown()
        {
            _timeout.Dispose();
        }

        [Test, Retry(3)]
        public async Task Scenario()
        {
            var exchange = new Exchange($"eventflow-{Guid.NewGuid():N}");
            using (var consumer = new RabbitMqConsumer(_uri, exchange, new[] { "#" }))
            using (var resolver = BuildResolver(exchange))
            {
                var commandBus = resolver.Resolve<ICommandBus>();
                var eventJsonSerializer = resolver.Resolve<IEventJsonSerializer>();

                var pingId = PingId.New;
                await commandBus.PublishAsync(new ThingyPingCommand(ThingyId.New, pingId), _timeout.Token).ConfigureAwait(false);

                var rabbitMqMessage = consumer.GetMessages(TimeSpan.FromMinutes(1)).Single();
                rabbitMqMessage.Exchange.Value.Should().Be(exchange.Value);
                rabbitMqMessage.RoutingKey.Value.Should().Be("eventflow.domainevent.thingy.thingy-ping.1");

                var pingEvent = (IDomainEvent<ThingyAggregate, ThingyId, ThingyPingEvent>)eventJsonSerializer.Deserialize(
                    rabbitMqMessage.Message,
                    new Metadata(rabbitMqMessage.Headers));

                pingEvent.AggregateEvent.PingId.Should().Be(pingId);
            }
        }

        [Test, Retry(3)]
        public async Task PublisherPerformance()
        {
            var exchange = new Exchange($"eventflow-{Guid.NewGuid():N}");
            var routingKey = new RoutingKey("performance");
            var exceptions = new ConcurrentBag<Exception>();
            const int taskCount = 10;
            const int messagesPrThread = 200;
            const int totalMessageCount = taskCount * messagesPrThread;

            using (var consumer = new RabbitMqConsumer(_uri, exchange, new[] { "#" }))
            using (var resolver = BuildResolver(exchange, o => o.RegisterServices(sr => sr.Register<ILog, NullLog>())))
            {
                var rabbitMqPublisher = resolver.Resolve<IRabbitMqPublisher>();
                var tasks = Enumerable.Range(0, taskCount)
                    .Select(i => Task.Run(() => SendMessagesAsync(rabbitMqPublisher, messagesPrThread, exchange, routingKey, exceptions, _timeout.Token)));

                await Task.WhenAll(tasks).ConfigureAwait(false);

                var rabbitMqMessages = consumer.GetMessages(TimeSpan.FromMinutes(1), totalMessageCount);
                rabbitMqMessages.Should().HaveCount(totalMessageCount);
                exceptions.Should().BeEmpty();
            }
        }

        private static async Task SendMessagesAsync(
            IRabbitMqPublisher rabbitMqPublisher,
            int count,
            Exchange exchange,
            RoutingKey routingKey,
            ConcurrentBag<Exception> exceptions,
            CancellationToken cancellationToken)
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
                    await rabbitMqPublisher.PublishAsync(cancellationToken, rabbitMqMessage).ConfigureAwait(false);
                }
            }
            catch (Exception e)
            {
                exceptions.Add(e);
            }
        }

        private IRootResolver BuildResolver(Exchange exchange, Func<IEventFlowOptions, IEventFlowOptions> configure = null)
        {
            configure = configure ?? (e => e);

            return configure(EventFlowOptions.New
                .PublishToRabbitMq(RabbitMqConfiguration.With(_uri, false, exchange: exchange.Value))
                .AddDefaults(EventFlowTestHelpers.Assembly))
                .RegisterServices(sr => sr.Register<IScopedContext, ScopedContext>(Lifetime.Scoped))
                .CreateResolver(false);
        }
    }
}
