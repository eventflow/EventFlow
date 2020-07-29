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
using EventFlow.Core;

namespace EventFlow.Sql.Connections
{
    public abstract class SqlConfiguration<T> : ISqlConfiguration<T>
        where T : ISqlConfiguration<T>
    {
        public string ConnectionString { get; private set; }

        public RetryDelay TransientRetryDelay { get; private set; } = RetryDelay.Between(
            TimeSpan.FromMilliseconds(50),
            TimeSpan.FromMilliseconds(100));

        public int TransientRetryCount { get; private set; } = 2;

        public T SetConnectionString(string connectionString)
        {
            ConnectionString = connectionString;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetTransientRetryDelay(RetryDelay retryDelay)
        {
            TransientRetryDelay = retryDelay;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }

        public T SetTransientRetryCount(int retryCount)
        {
            TransientRetryCount = retryCount;

            // Are there alternatives to this double cast?
            return (T)(object)this;
        }
    }
}