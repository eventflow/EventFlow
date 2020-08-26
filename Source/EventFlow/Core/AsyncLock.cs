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
using System.Threading;
using System.Threading.Tasks;

namespace EventFlow.Core
{
    public sealed class AsyncLock : IDisposable
    {
        private sealed class AsyncLockReleaser : IDisposable
        {
            private readonly AsyncLock _asyncLock;

            public AsyncLockReleaser(AsyncLock asyncLock)
            {
                _asyncLock = asyncLock;
            }

            public void Dispose()
            {
                _asyncLock.Release();
            }
        }

        private readonly Task<IDisposable> _completed;
        private readonly AsyncLockReleaser _releaser;
        private readonly SemaphoreSlim _semaphore;

        public AsyncLock(int initialCount = 1)
        {
            _semaphore = new SemaphoreSlim(initialCount);
            _releaser = new AsyncLockReleaser(this);
            _completed = Task.FromResult<IDisposable>(_releaser);
        }

        public Task<IDisposable> WaitAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.Run(() => null as IDisposable, cancellationToken);
            }

            // ReSharper disable once MethodSupportsCancellation
            var task = _semaphore.WaitAsync();

            if (task.Status == TaskStatus.RanToCompletion)
            {
                return _completed;
            }

            if (!cancellationToken.CanBeCanceled)
            {
                return task.ContinueWith(
                    (_, s) => (IDisposable)s,
                    _releaser,
                    CancellationToken.None,
                    TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default);
            }

            var taskCompletionSource = new TaskCompletionSource<IDisposable>();
            var registration = cancellationToken.Register(
                () =>
                {
                    if (taskCompletionSource.TrySetCanceled())
                    {
                        task.ContinueWith(
                            (_, s) => ((SemaphoreSlim)s).Release(),
                            _semaphore,
                            CancellationToken.None,
                            TaskContinuationOptions.ExecuteSynchronously,
                            TaskScheduler.Default);
                    }
                });

            task.ContinueWith(
                _ =>
                    {
                        if (taskCompletionSource.TrySetResult(_releaser))
                        {
                            registration.Dispose();
                        }
                    },
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);

            return taskCompletionSource.Task;
        }

        public void Release()
        {
            _semaphore.Release();
        }

        public IDisposable Wait(CancellationToken cancellationToken)
        {
            _semaphore.Wait(cancellationToken);
            return _releaser;
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
