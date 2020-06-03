// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Exceptions;
using EventFlow.Logs;

namespace EventFlow.EventStores.Files
{
    public class FilesEventPersistence : FilesEventPersistence<string>
    {
        public FilesEventPersistence(ILog log,
            IJsonSerializer jsonSerializer,
            IFilesEventStoreConfiguration configuration,
            IFilesEventLocator filesEventLocator)
            : base(log, jsonSerializer, configuration, filesEventLocator)
        {
        }
    }

    public class FilesEventPersistence<TSerialized> : IEventPersistence<TSerialized>
    {
        private readonly ILog _log;
        private readonly ISerializer<TSerialized> _serializer;
        private readonly IFilesEventLocator _filesEventLocator;
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly string _logFilePath;
        private long _globalSequenceNumber;
        private readonly Dictionary<long, string> _eventLog;

        private class FileEventData : ICommittedDomainEvent<TSerialized>
        {
            public long GlobalSequenceNumber { get; set; }
            public string AggregateId { get; set; }
            public TSerialized Data { get; set; }
            public TSerialized Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public class EventStoreLog
        {
            public long GlobalSequenceNumber { get; set; }
            public Dictionary<long, string> Log { get; set; }
        }

        public FilesEventPersistence(ILog log,
            ISerializer<TSerialized> serializer,
            IFilesEventStoreConfiguration configuration,
            IFilesEventLocator filesEventLocator)
        {
            _log = log;
            _serializer = serializer;
            _filesEventLocator = filesEventLocator;
            _logFilePath = Path.Combine(configuration.StorePath, "Log.store");

            if (TryLoadFileData<EventStoreLog>(_logFilePath, out var eventStoreLog))
            {
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

        public async Task<AllCommittedEventsPage<TSerialized>> LoadAllCommittedEvents(GlobalPosition globalPosition,
            int pageSize,
            CancellationToken cancellationToken)
        {
            var startPosition = globalPosition.IsStart
                ? 1
                : int.Parse(globalPosition.Value);

            var committedDomainEvents = new List<FileEventData>();

            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var paths = EnumeratePaths(startPosition).Take(pageSize);

                foreach (var path in paths)
                {
                    if (TryLoadFileData<FileEventData>(_logFilePath, out var committedDomainEvent))
                    {
                        committedDomainEvents.Add(committedDomainEvent);
                    }
                }
            }

            var nextPosition = committedDomainEvents.Any()
                ? committedDomainEvents.Max(e => e.GlobalSequenceNumber) + 1
                : startPosition;

            return new AllCommittedEventsPage<TSerialized>(new GlobalPosition(nextPosition.ToString()), committedDomainEvents);
        }

        private IEnumerable<string> EnumeratePaths(long startPosition)
        {
            while (_eventLog.TryGetValue(startPosition, out var path))
            {
                if (File.Exists(path))
                {
                    yield return path;
                }

                startPosition++;
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent<TSerialized>>> CommitEventsAsync(IIdentity id,
            IReadOnlyCollection<SerializedEvent<TSerialized>> serializedEvents,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var committedDomainEvents = new List<ICommittedDomainEvent<TSerialized>>();

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

                    var json = _serializer.Serialize(fileEventData, true);
                    _log.Verbose("Writing file '{0}'", eventPath);

                    if (File.Exists(eventPath))
                    {
                        throw new OptimisticConcurrencyException(
                            $"Event {fileEventData.AggregateSequenceNumber} already exists for entity with ID '{fileEventData.AggregateId}'");
                    }

                    TryWriteFileData(eventPath, fileEventData);

                    committedDomainEvents.Add(fileEventData);
                }

                var eventStoreLog = new EventStoreLog
                {
                    GlobalSequenceNumber = _globalSequenceNumber,
                    Log = _eventLog
                };

                TryWriteFileData(_logFilePath, eventStoreLog);

                _log.Verbose(
                    "Writing global sequence number '{0}' to '{1}'",
                    _globalSequenceNumber,
                    _logFilePath);

                return committedDomainEvents;
            }
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent<TSerialized>>> LoadCommittedEventsAsync(IIdentity id,
            int fromEventSequenceNumber,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var committedDomainEvents = new List<ICommittedDomainEvent<TSerialized>>();
                for (var i = fromEventSequenceNumber;; i++)
                {
                    var eventPath = _filesEventLocator.GetEventPath(id, i);

                    if (TryLoadFileData<FileEventData>(eventPath, out var committedDomainEvent))
                    {
                        committedDomainEvents.Add(committedDomainEvent);
                    }
                }
            }
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            _log.Verbose("Deleting entity with ID '{0}'", id);
            var path = _filesEventLocator.GetEntityPath(id);
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                Directory.Delete(path, true);
            }
        }

        private bool TryLoadFileData<T>(string path, out T data)
        {
            data = default;

            if (File.Exists(_logFilePath))
            {
                switch (_serializer)
                {
                    case ISerializer<byte[]> binarySerializer:
                    {
                        data = binarySerializer.Deserialize<T>(File.ReadAllBytes(path));
                        return true;
                    }
                    case ISerializer<string> stringSerializer:
                    {
                        data = stringSerializer.Deserialize<T>(File.ReadAllText(path));
                        return true;
                    }
                }
            }

            return false;
        }

        private bool TryWriteFileData<T>(string path, T data)
        {
            switch (_serializer)
            {
                case ISerializer<byte[]> binarySerializer:
                {
                    File.WriteAllBytes(path, binarySerializer.Serialize(data));
                    return true;
                }
                case ISerializer<string> stringSerializer:
                {
                    File.WriteAllText(path, stringSerializer.Serialize(data, true));
                    return true;
                }
            }

            return false;
        }

        private EventStoreLog RecreateEventStoreLog(string path)
        {
            var directory = Directory.GetDirectories(path)
                .SelectMany(Directory.GetDirectories)
                .SelectMany(Directory.GetFiles)
                .Select(f =>
                {
                    Console.WriteLine(f);

                    TryLoadFileData<FileEventData>(f, out var fileEventData);
                    return new {fileEventData.GlobalSequenceNumber, Path = f};
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
