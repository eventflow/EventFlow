﻿// The MIT License (MIT)
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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;

namespace EventFlow.EventStores.Files
{
    public class FilesEventPersistence : IEventPersistence
    {
        private readonly ILog _log;
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFilesEventLocator _filesEventLocator;
        private readonly IFileSystem _fileSystem;
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly string _logFilePath;
        private long _globalSequenceNumber;
        private readonly Dictionary<long, string> _eventLog;

        public class FileEventData : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
            public string AggregateId { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public class EventStoreLog
        {
            public long GlobalSequenceNumber { get; set; }
            public Dictionary<long, string> Log { get; set; }
        }

        public FilesEventPersistence(
            ILog log,
            IJsonSerializer jsonSerializer,
            IFilesEventStoreConfiguration configuration,
            IFilesEventLocator filesEventLocator,
            IFileSystem fileSystem)
        {
            _log = log;
            _jsonSerializer = jsonSerializer;
            _filesEventLocator = filesEventLocator;
            _fileSystem = fileSystem;
            _logFilePath = Path.Combine(configuration.StorePath, "Log.store");

            if (AsyncHelper.Run(() => _fileSystem.FileExistAsync(_logFilePath, CancellationToken.None)))
            {
                var json = AsyncHelper.Run(() => _fileSystem.ReadAllTextAsync(_logFilePath, CancellationToken.None));
                var eventStoreLog = _jsonSerializer.Deserialize<EventStoreLog>(json);
                _globalSequenceNumber = eventStoreLog.GlobalSequenceNumber;
                _eventLog = eventStoreLog.Log ?? new Dictionary<long, string>();

                if (_eventLog.Count != _globalSequenceNumber)
                {
                    eventStoreLog = RecreateEventStoreLog(configuration.StorePath);
                    _globalSequenceNumber = eventStoreLog.GlobalSequenceNumber;
                    _eventLog = eventStoreLog.Log;
                }
            }
            else
            {
                _eventLog = new Dictionary<long, string>();
            }
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(
            GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPostion = globalPosition.IsStart
                ? 1
                : int.Parse(globalPosition.Value);

            var paths = Enumerable.Range(startPostion, pageSize)
                .TakeWhile(g => _eventLog.ContainsKey(g))
                .Select(g => _eventLog[g])
                .ToList();

            var committedDomainEvents = new List<FileEventData>();
            foreach (var path in paths)
            {
                var committedDomainEvent = await LoadFileEventDataFile(path, cancellationToken).ConfigureAwait(false);
                committedDomainEvents.Add(committedDomainEvent);
            }

            var nextPosition = committedDomainEvents.Any()
                ? committedDomainEvents.Max(e => e.GlobalSequenceNumber) + 1
                : startPostion;

            return new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), committedDomainEvents);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(
            IIdentity id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var committedDomainEvents = new List<ICommittedDomainEvent>();

                var aggregatePath = _filesEventLocator.GetEntityPath(id);
                if (!Directory.Exists(aggregatePath))
                {
                    Directory.CreateDirectory(aggregatePath);
                }

                foreach (var serializedEvent in serializedEvents)
                {
                    var eventPath = _filesEventLocator.GetEventPath(id, serializedEvent.AggregateSequenceNumber);
                    _globalSequenceNumber++;
                    _eventLog[_globalSequenceNumber] = eventPath;

                    var fileEventData = new FileEventData
                        {
                            AggregateId = id.Value,
                            AggregateSequenceNumber = serializedEvent.AggregateSequenceNumber,
                            Data = serializedEvent.SerializedData,
                            Metadata = serializedEvent.SerializedMetadata,
                            GlobalSequenceNumber = _globalSequenceNumber,
                        };
            
                    var json = _jsonSerializer.Serialize(fileEventData, true);

                    if (await _fileSystem.FileExistAsync(eventPath, cancellationToken).ConfigureAwait(false))
                    {
                        // TODO: This needs to be on file creation
                        throw new OptimisticConcurrencyException(string.Format(
                            "Event {0} already exists for entity with ID '{1}'",
                            fileEventData.AggregateSequenceNumber,
                            id));
                    }

                    _log.Verbose("Writing file '{0}'", eventPath);
                    await _fileSystem.WriteAllTextAsync(eventPath, json, false, cancellationToken).ConfigureAwait(false);

                    committedDomainEvents.Add(fileEventData);
                }

                _log.Verbose(
                    "Writing global sequence number '{0}' to '{1}'",
                    _globalSequenceNumber,
                    _logFilePath);
                var logFileJson = _jsonSerializer.Serialize(
                    new EventStoreLog
                    {
                        GlobalSequenceNumber = _globalSequenceNumber,
                        Log = _eventLog,
                    },
                    true);
                await _fileSystem.WriteAllTextAsync(
                    _logFilePath,
                    logFileJson,
                    true,
                    CancellationToken.None /* don't allow cancellation here */)
                    .ConfigureAwait(false);

                return committedDomainEvents;
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(
            IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var committedDomainEvents = new List<ICommittedDomainEvent>();
                for (var i = fromEventSequenceNumber;; i++)
                {
                    var eventPath = _filesEventLocator.GetEventPath(id, i);
                    if (!await _fileSystem.FileExistAsync(eventPath, cancellationToken).ConfigureAwait(false))
                    {
                        return committedDomainEvents;
                    }

                    var committedDomainEvent = await LoadFileEventDataFile(eventPath, cancellationToken).ConfigureAwait(false);
                    committedDomainEvents.Add(committedDomainEvent);
                }
            }
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            _log.Verbose("Deleting entity with ID '{0}'", id);
            var path = _filesEventLocator.GetEntityPath(id);
            await _fileSystem.DeleteDirectoryAsync(path, true, cancellationToken).ConfigureAwait(false);
        }

        private async Task<FileEventData> LoadFileEventDataFile(string eventPath, CancellationToken cancellationToken)
        {
            var json = await _fileSystem.ReadAllTextAsync(eventPath, cancellationToken).ConfigureAwait(false);
            return _jsonSerializer.Deserialize<FileEventData>(json);
        }

        private EventStoreLog RecreateEventStoreLog(string path)
        {
            var directory = Directory.GetDirectories(path)
                .SelectMany(Directory.GetDirectories)
                .SelectMany(Directory.GetFiles)
                .Select(f =>
                    {
                        _log.Verbose($"Reading event data: {f}");

                        var json = AsyncHelper.Run(() => _fileSystem.ReadAllTextAsync(f, CancellationToken.None));
                        var fileEventData = _jsonSerializer.Deserialize<FileEventData>(json);
                        return new { fileEventData.GlobalSequenceNumber, Path = f };
                    })
                .ToDictionary(a => a.GlobalSequenceNumber, a => a.Path);

            return new EventStoreLog
                {
                    GlobalSequenceNumber = directory.Keys.Any() ? directory.Keys.Max() : 0,
                    Log = directory,
                };
        }
    }
}