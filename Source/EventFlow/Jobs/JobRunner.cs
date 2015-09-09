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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;

namespace EventFlow.Jobs
{
    public class JobRunner : IJobRunner
    {
        private readonly IResolver _resolver;
        private readonly IJsonSerializer _jsonSerializer;

        public JobRunner(
            IResolver resolver,
            IJsonSerializer jsonSerializer)
        {
            _resolver = resolver;
            _jsonSerializer = jsonSerializer;
        }

        public void Execute(string serializedJob, string jobType)
        {
            Execute(serializedJob, jobType, CancellationToken.None);
        }

        public void Execute(string serializedJob, string jobType, CancellationToken cancellationToken)
        {
            using (var a = AsyncHelper.Wait)
            {
                a.Run(ExecuteAsync(serializedJob, jobType, cancellationToken));
            }
        }

        public Task ExecuteAsync(string serializedJob, string jobType, CancellationToken cancellationToken)
        {
            var executeCommandJob = _jsonSerializer.Deserialize<ExecuteCommandJob>(serializedJob);
            return executeCommandJob.ExecuteAsync(_resolver);
        }
    }
}
