// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using Microsoft.Extensions.Logging;

namespace EventFlow.TestHelpers.Aggregates.Queries
{
    public class ScopedContext : IScopedContext, IDisposable
    {
        private readonly ILogger<ScopedContext> _logger;
        private bool _isDisposed;

        public string Id { get; } = Guid.NewGuid().ToString("D");

        public ScopedContext(
            ILogger<ScopedContext> logger)
        {
            _logger = logger;
            _logger.LogInformation("Scoped context {Id} created", Id);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException($"Scoped context {Id} is already disposed");
            }

            _isDisposed = true;

            _logger.LogInformation("Scoped context {Id} was disposed", Id);
        }
    }
}
