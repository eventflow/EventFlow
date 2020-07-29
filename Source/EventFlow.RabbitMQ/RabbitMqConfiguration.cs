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

namespace EventFlow.RabbitMQ
{
    public class RabbitMqConfiguration : IRabbitMqConfiguration
    {
        public Uri Uri { get; }
        public bool Persistent { get; }
        public int ModelsPrConnection { get; }
        public string Exchange { get; }

        public static IRabbitMqConfiguration With(
            Uri uri,
            bool persistent = true,
            int modelsPrConnection = 5,
            string exchange = "eventflow")
        {
            return new RabbitMqConfiguration(uri, persistent, modelsPrConnection, exchange);
        }

        private RabbitMqConfiguration(Uri uri, bool persistent, int modelsPrConnection, string exchange)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (string.IsNullOrEmpty(exchange)) throw new ArgumentNullException(nameof(exchange));

            Uri = uri;
            Persistent = persistent;
            ModelsPrConnection = modelsPrConnection;
            Exchange = exchange;
        }
    }
}