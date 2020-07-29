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
using System.Collections.Generic;

namespace EventFlow.RabbitMQ.Integrations
{
    public class RabbitMqMessage
    {
        public MessageId MessageId { get; }
        public string Message { get; }
        public IReadOnlyDictionary<string, string> Headers { get; }
        public Exchange Exchange { get; }
        public RoutingKey RoutingKey { get; }

        public RabbitMqMessage(
            string message,
            IReadOnlyDictionary<string, string> headers,
            Exchange exchange,
            RoutingKey routingKey,
            MessageId messageId)
        {
            if (string.IsNullOrEmpty(message)) throw new ArgumentNullException(nameof(message));
            if (headers == null) throw new ArgumentNullException(nameof(headers));
            if (exchange == null) throw new ArgumentNullException(nameof(exchange));
            if (routingKey == null) throw new ArgumentNullException(nameof(routingKey));
            if (messageId == null) throw new ArgumentNullException(nameof(messageId));

            Message = message;
            Headers = headers;
            Exchange = exchange;
            RoutingKey = routingKey;
            MessageId = messageId;
        }

        public override string ToString()
        {
            return $"{{Exchange: {Exchange}, RoutingKey: {RoutingKey}, MessageId: {MessageId}, Headers: {Headers.Count}, Bytes: {Message.Length/2}}}";
        }
    }
}