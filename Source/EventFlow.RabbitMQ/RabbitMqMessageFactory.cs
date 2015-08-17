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

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.EventStores;

namespace EventFlow.RabbitMQ
{
    public class RabbitMqMessageFactory : IRabbitMqMessageFactory
    {
        private readonly IEventJsonSerializer _eventJsonSerializer;

        public RabbitMqMessageFactory(
            IEventJsonSerializer eventJsonSerializer)
        {
            _eventJsonSerializer = eventJsonSerializer;
        }

        public Task<RabbitMqMessage> CreateMessageAsync(
            IDomainEvent domainEvent,
            CancellationToken cancellationToken)
        {
            var headers = domainEvent.Metadata
                .ToDictionary(kv => string.Format("eventflow-metadata-{0}", kv.Key), kv => kv.Value);
            headers.Add("eventflow-encoding", "utf8");

            var serializedEvent = _eventJsonSerializer.Serialize(
                domainEvent.GetAggregateEvent(),
                Enumerable.Empty<KeyValuePair<string, string>>());

            var message = Encoding.UTF8.GetBytes(serializedEvent.SerializedData);

            return Task.FromResult(new RabbitMqMessage(message, headers));
        }
    }
}
