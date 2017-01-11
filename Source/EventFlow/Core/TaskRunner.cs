// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Logs;

namespace EventFlow.Core
{
    public class TaskRunner : ITaskRunner
    {
        private readonly ILog _log;
        private readonly IResolver _resolver;

        public TaskRunner(
            ILog log,
            IResolver resolver)
        {
            _log = log;
            _resolver = resolver;
        }

        public void Run(
            Label label,
            Func<IResolver, CancellationToken, Task> taskFactory,
            CancellationToken cancellationToken)
        {
            var scopeResolver = _resolver.Resolve<IScopeResolver>();

            Task.Run(
                () => RunAsync(label, scopeResolver, taskFactory, cancellationToken),
                CancellationToken.None /* no mistake */);
        }

        private async Task RunAsync(
            Label label,
            IScopeResolver scopeResolver,
            Func<IResolver, CancellationToken, Task> taskFactory,
            CancellationToken cancellationToken)
        {
            using (scopeResolver)
            {
                var taskId = Guid.NewGuid().ToString("N");
                var stopwatch = Stopwatch.StartNew();

                _log.Verbose($"Starting task '{label}' ({taskId})");

                try
                {
                    await taskFactory(scopeResolver, cancellationToken).ConfigureAwait(false);
                    _log.Verbose($"Task '{label}' ({taskId}) completed after {stopwatch.Elapsed.TotalSeconds:0.###} seconds");
                }
                catch (Exception e)
                {
                    _log.Error(e, $"Task '{label}' ({taskId}) failed after {stopwatch.Elapsed.TotalSeconds:0.###} seconds with exception '{e.GetType().Name}': {e.Message}");
                }
            }
        }
    }
}