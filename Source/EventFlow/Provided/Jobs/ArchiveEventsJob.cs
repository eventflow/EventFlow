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

using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventArchives;
using EventFlow.Jobs;
using EventFlow.ValueObjects;

namespace EventFlow.Provided.Jobs
{
    [JobVersion("ArchiveEvents", 1)]
    public class ArchiveEventsJob : IJob
    {
        public ArchiveEventsJob(
            IIdentity id)
        {
            Id = id.Value;
        }

        public string Id { get; }

        public Task ExecuteAsync(
            IResolver resolver,
            CancellationToken cancellationToken)
        {
            var eventArchive = resolver.Resolve<IEventArchive>();

            return eventArchive.ArchiveAsync(
                new EventsId(Id),
                cancellationToken);
        }

        private class EventsId : SingleValueObject<string>, IIdentity
        {
            public EventsId(string value) : base(value)
            {
            }
        }
    }
}