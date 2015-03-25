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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventFlow.Core
{
    public static class Retry
    {
        public static Task ThisAsync(
            Func<Task> task,
            int retries = 2,
            IEnumerable<Type> transientExceptionTypes = null,
            TimeSpan delayBeforeRetry = default(TimeSpan))
        {
            return ThisAsync(
                async () =>
                    {
                        await task().ConfigureAwait(false);
                        return 0;
                    },
                retries,
                transientExceptionTypes,
                delayBeforeRetry);
        }

        public static async Task<T> ThisAsync<T>(
            Func<Task<T>> task,
            int retries = 2,
            IEnumerable<Type> transientExceptionTypes = null,
            TimeSpan delayBeforeRetry = default(TimeSpan))
        {
            if (task == null) throw new ArgumentNullException("task");
            if (retries <= 0) throw new ArgumentException(string.Format("It doesn't make any sense to have a total retries of {0}", retries), "retries");
            if (delayBeforeRetry.Ticks < 0) throw new ArgumentOutOfRangeException("delayBeforeRetry", "Please specify zero or a positive delay");

            var exceptionList = (transientExceptionTypes ?? Enumerable.Empty<Type>()).ToList();
            var currentCount = 1;

            while (true)
            {
                try
                {
                    if (currentCount > 1 && delayBeforeRetry != default(TimeSpan))
                    {
                        await Task.Delay(delayBeforeRetry);
                    }

                    return await task().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (currentCount <= retries && exceptionList.Any(t => exception.GetType() == t))
                    {
                        currentCount++;
                        continue;
                    }

                    throw;
                }
            }
        }
    }
}
