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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Logs;
using Newtonsoft.Json;

namespace EventFlow.EventArchives.Files
{
    public class FileEventArchivePersistance : IEventArchivePersistance
    {
        private readonly IFileSystem _fileSystem;
        private readonly IFileEventArchiveConfiguration _configuration;
        private readonly IEventArchiveStreamFormatter _eventArchiveStreamFormatter;
        private readonly ILog _log;

        public FileEventArchivePersistance(
            ILog log,
            IFileSystem fileSystem,
            IFileEventArchiveConfiguration configuration,
            IEventArchiveStreamFormatter eventArchiveStreamFormatter)
        {
            _log = log;
            _fileSystem = fileSystem;
            _configuration = configuration;
            _eventArchiveStreamFormatter = eventArchiveStreamFormatter;
        }

        public async Task<EventArchiveDetails> ArchiveAsync(
            IIdentity identity,
            Func<CancellationToken, Task<IReadOnlyCollection<ICommittedDomainEvent>>> batchFetcher,
            CancellationToken cancellationToken)
        {
            var fileName = _configuration.GetEventArchiveFile(identity);

            _log.Verbose($"Storing event archive for '{identity}' at '{fileName}'");
            var stopwatch = Stopwatch.StartNew();

            using (var stream = await _fileSystem.CreateAsync(
                    fileName,
                    false,
                    cancellationToken)
                .ConfigureAwait(false))
            {
                await _eventArchiveStreamFormatter.StreamEventsAsync(
                    stream,
                    batchFetcher,
                    cancellationToken)
                    .ConfigureAwait(false);
            }

            _log.Verbose($"Used {stopwatch.Elapsed.TotalSeconds:0.###} seconds to store archive of '{identity}' at '{fileName}'");

            return new EventArchiveDetails(new Uri(fileName));
        }
    }
}