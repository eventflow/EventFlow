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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventCaches;
using EventFlow.Exceptions;
using EventFlow.Logs;

namespace EventFlow.EventStores.Files
{
    public class FilesEventStore : EventStore
    {
        private readonly IJsonSerializer _jsonSerializer;
        private readonly IFilesEventStoreConfiguration _configuration;
        private readonly AsyncLock _asyncLock = new AsyncLock();
        private readonly string _globalSequenceNumberFilePath;
        private long _globalSequenceNumber;

        public class FileEventData : ICommittedDomainEvent
        {
            public long GlobalSequenceNumber { get; set; }
            public Guid BatchId { get; set; }
            public string AggregateId { get; set; }
            public string AggregateName { get; set; }
            public string Data { get; set; }
            public string Metadata { get; set; }
            public int AggregateSequenceNumber { get; set; }
        }

        public FilesEventStore(
            ILog log,
            IAggregateFactory aggregateFactory,
            IEventJsonSerializer eventJsonSerializer,
            IEnumerable<IMetadataProvider> metadataProviders,
            IJsonSerializer jsonSerializer,
            IEventCache eventCache,
            IFilesEventStoreConfiguration configuration)
            : base(log, aggregateFactory, eventJsonSerializer, eventCache, metadataProviders)
        {
            _jsonSerializer = jsonSerializer;
            _configuration = configuration;
            _globalSequenceNumberFilePath = Path.Combine(_configuration.StorePath, "GlobalSequenceNumber.store");

            if (File.Exists(_globalSequenceNumberFilePath))
            {
                _globalSequenceNumber = long.Parse(File.ReadAllText(_globalSequenceNumberFilePath));
            }
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync<TAggregate>(
            string id,
            IReadOnlyCollection<SerializedEvent> serializedEvents,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var aggregateType = typeof (TAggregate);
                var batchId = Guid.NewGuid();
                var committedDomainEvents = new List<ICommittedDomainEvent>();

                var aggregatePath = GetAggregatePath(aggregateType, id);
                if (!Directory.Exists(aggregatePath))
                {
                    Directory.CreateDirectory(aggregatePath);
                }

                foreach (var serializedEvent in serializedEvents)
                {
                    _globalSequenceNumber++;
                    var fileEventData = new FileEventData
                        {
                            AggregateId = id,
                            AggregateName = aggregateType.Name,
                            AggregateSequenceNumber = serializedEvent.AggregateSequenceNumber,
                            BatchId = batchId,
                            Data = serializedEvent.Data,
                            GlobalSequenceNumber = _globalSequenceNumber,
                            Metadata = serializedEvent.Meta,
                        };
            
                    var json = _jsonSerializer.Serialize(fileEventData);
                    var eventPath = GetEventPath(aggregateType, id, serializedEvent.AggregateSequenceNumber);

                    if (File.Exists(eventPath))
                    {
                        // TODO: This needs to be on file creation
                        throw new OptimisticConcurrencyException(string.Format(
                            "Event {0} already exists for aggregate '{1}' with ID '{2}'",
                            fileEventData.AggregateSequenceNumber,
                            aggregateType.Name,
                            id));
                    }

                    using (var streamWriter = File.CreateText(eventPath))
                    {
                        Log.Verbose("Writing file '{0}'", eventPath);
                        await streamWriter.WriteAsync(json).ConfigureAwait(false);
                    }

                    committedDomainEvents.Add(fileEventData);
                }

                using (var streamWriter = File.CreateText(_globalSequenceNumberFilePath))
                {
                    Log.Verbose(
                        "Writing global sequence number '{0}' to '{1}'",
                        _globalSequenceNumber,
                        _globalSequenceNumberFilePath);
                    await streamWriter.WriteAsync(_globalSequenceNumber.ToString()).ConfigureAwait(false);
                }

                return committedDomainEvents;
            }
        }

        protected override async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync<TAggregate>(
            string id,
            CancellationToken cancellationToken)
        {
            using (await _asyncLock.WaitAsync(cancellationToken).ConfigureAwait(false))
            {
                var aggregateType = typeof(TAggregate);
                var committedDomainEvents = new List<ICommittedDomainEvent>();
                for (var i = 1; ; i++)
                {
                    var eventPath = GetEventPath(aggregateType, id, i);
                    if (!File.Exists(eventPath))
                    {
                        return committedDomainEvents;
                    }

                    using (var streamReader = File.OpenText(eventPath))
                    {
                        var json = await streamReader.ReadToEndAsync().ConfigureAwait(false);
                        var committedDomainEvent = _jsonSerializer.Deserialize<FileEventData>(json);
                        committedDomainEvents.Add(committedDomainEvent);
                    }
                }
            }
        }

        private string GetAggregatePath(Type aggregateType, string id)
        {
            return Path.Combine(
                _configuration.StorePath,
                aggregateType.Name,
                id);
        }

        private string GetEventPath(Type aggregateType, string id, int aggregateSequenceNumber)
        {
            return Path.Combine(
                GetAggregatePath(aggregateType, id),
                string.Format("{0}.json", aggregateSequenceNumber));
        }
    }
}
