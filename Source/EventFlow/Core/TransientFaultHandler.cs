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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.Core
{
    public class TransientFaultHandler : ITransientFaultHandler
    {
        private readonly IResolver _resolver;
        private readonly ILog _log;
        private IRetryStrategy _retryStrategy;

        public TransientFaultHandler(
            IResolver resolver,
            ILog log)
        {
            _resolver = resolver;
            _log = log;
        }

        public void Use<TRetryStrategy>(Action<TRetryStrategy> configureStrategy = null) where TRetryStrategy : IRetryStrategy
        {
            if (_retryStrategy != null) throw new InvalidOperationException(string.Format(
                "Retry stratety has already been configured as a '{0}' strategy", _retryStrategy.GetType().Name));

            var retryStrategy = _resolver.Resolve<TRetryStrategy>();
            if (configureStrategy != null)
            {
                configureStrategy(retryStrategy);
            }

            _retryStrategy = retryStrategy;
        }

        public async Task<T> TryAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            var currentRetryCount = 0;

            while (true)
            {
                Exception currentException;
                Retry retry;
                try
                {
                    var result = await action(cancellationToken).ConfigureAwait(false);
                    _log.Verbose(
                        "Finished execution after {0} retries and {1:0.###} seconds",
                        currentRetryCount,
                        stopwatch.Elapsed.TotalSeconds);
                    return result;
                }
                catch (Exception exception)
                {
                    currentException = exception;
                    var currentTime = stopwatch.Elapsed;
                    retry = _retryStrategy.ShouldThisBeRetried(currentException, currentTime, currentRetryCount);
                    if (!retry.ShouldBeRetried)
                    {
                        throw;
                    }
                }

                currentRetryCount++;
                if (retry.RetryAfter != TimeSpan.Zero)
                {
                    _log.Verbose(
                        "Exception {0} with message {1} is transient, retrying after {2:0.###} seconds for retry count {3}",
                        currentException.GetType().Name,
                        currentException.Message,
                        retry.RetryAfter.TotalSeconds,
                        currentRetryCount);
                    await Task.Delay(retry.RetryAfter, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    _log.Verbose(
                        "Exception {0} with message {1} is transient, retrying NOW for retry count {2}",
                        currentException.GetType().Name,
                        currentException.Message,
                        currentRetryCount);
                }
            }
        }
    }
}
