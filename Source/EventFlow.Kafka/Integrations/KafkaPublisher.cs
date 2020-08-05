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
using EventFlow.Core;
using EventFlow.Logs;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Kafka.Integrations
{
    public class KafkaPublisher : IKafkaPublisher
    {
        private readonly ILog _log;
        private readonly IKafkaProducerFactory _producerFactory;
        private readonly ProducerConfig _configuration;
        private readonly ITransientFaultHandler<IKafkaRetryStrategy> _transientFaultHandler;

        public KafkaPublisher(ILog log,
            IKafkaProducerFactory producerFactory,
            ProducerConfig configuration,
            ITransientFaultHandler<IKafkaRetryStrategy> transientFaultHandler)
        {
            _log = log;
            _producerFactory = producerFactory;
            _configuration = configuration;
            _transientFaultHandler = transientFaultHandler;
        }

        public async Task PublishAsync(IReadOnlyCollection<KafkaMessage> kafkaMessages, CancellationToken cancellationToken)
        {
            try
            {

                await _transientFaultHandler.TryAsync(c => (
                        ProduceMessages(kafkaMessages, c)),
                            Label.Named("kafkas-publish"),
                            cancellationToken)
                    .ConfigureAwait(false);

            }
            catch (OperationCanceledException e)
            {
                _log.Error(e, "Failed to publish domain events to Kafka");
                throw e;
            }

        }

        private async Task ProduceMessages(IReadOnlyCollection<KafkaMessage> kafkaMessages, CancellationToken c)
        {
            var timeout = TimeSpan.FromMilliseconds(_configuration.TransactionTimeoutMs ?? 2000);

            IProducer<string, KafkaMessage> kafkaProducer = null;
            _log.Verbose(
                    "Publishing {0} domain events to Kafka brokers '{1}'",
                    kafkaMessages.Count, _configuration.BootstrapServers);

            try
            {
                kafkaProducer = _producerFactory.CreateProducer();

                kafkaProducer.BeginTransaction();
                foreach (var message in kafkaMessages)
                {
                    var kafkaDomainMessage = new Message<string, KafkaMessage> { Value = message, Key = message.MessageId.Value };
                    await kafkaProducer.ProduceAsync(message.Topic, kafkaDomainMessage, c);
                }
                kafkaProducer.CommitTransaction(timeout);
            }
            catch (Exception)
            {
                if (kafkaProducer != null)
                {
                    kafkaProducer.AbortTransaction(timeout);
                    kafkaProducer.Dispose();
                }
                throw;
            }
        }
    }
}
