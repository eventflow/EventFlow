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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.MongoDB.ValueObjects;
using MongoDB.Driver;

namespace EventFlow.MongoDB.EventStore
{
    public class MongoDbEventPersistence : IEventPersistence
    {
        public static readonly string CollectionName = "eventflow.events";
        private readonly ILog _log;
        private readonly IMongoDatabase _mongoDatabase;
        private readonly IMongoDbEventSequenceStore _mongoDbEventSequenceStore;

        public MongoDbEventPersistence(ILog log, IMongoDatabase mongoDatabase, IMongoDbEventSequenceStore mongoDbEventSequenceStore)
        {
            _log = log;
            _mongoDatabase = mongoDatabase;
            _mongoDbEventSequenceStore = mongoDbEventSequenceStore;
        }

        public async Task<AllCommittedEventsPage> LoadAllCommittedEvents(GlobalPosition globalPosition, int pageSize, CancellationToken cancellationToken)
        {
            long startPosition = globalPosition.IsStart
                ? 0
                : long.Parse(globalPosition.Value);

            List<MongoDbEventDataModel> eventDataModels = await MongoDbEventStoreCollection
                .Find(model => model._id >= startPosition)
                .Limit(pageSize)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
            
            long nextPosition = eventDataModels.Any()
                ? eventDataModels.Max(e => e._id) + 1
                : startPosition;

            return new AllCommittedEventsPage(new GlobalPosition(nextPosition.ToString()), eventDataModels);
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> CommitEventsAsync(IIdentity id, IReadOnlyCollection<SerializedEvent> serializedEvents, CancellationToken cancellationToken)
        {
            if (!serializedEvents.Any())
            {
                return new ICommittedDomainEvent[] { };
            }

            var eventDataModels = serializedEvents
                .Select((e, i) => new MongoDbEventDataModel
                {
                    _id = _mongoDbEventSequenceStore.GetNextSequence(CollectionName),
                    AggregateId = id.Value,
                    AggregateName = e.Metadata[MetadataKeys.AggregateName],
                    BatchId = Guid.Parse(e.Metadata[MetadataKeys.BatchId]),
                    Data = e.SerializedData,
                    Metadata = e.SerializedMetadata,
                    AggregateSequenceNumber = e.AggregateSequenceNumber
                })
                .OrderBy(x => x.AggregateSequenceNumber)
                .ToList();

            _log.Verbose("Committing {0} events to MongoDb event store for entity with ID '{1}'", eventDataModels.Count, id);
            try
            {
                await MongoDbEventStoreCollection
                    .InsertManyAsync(eventDataModels, cancellationToken: cancellationToken)
                    .ConfigureAwait(continueOnCapturedContext: false);
            }
            catch (MongoBulkWriteException e)
            {
                throw new OptimisticConcurrencyException(e.Message, e);

            }
            return eventDataModels;
        }

        public async Task<IReadOnlyCollection<ICommittedDomainEvent>> LoadCommittedEventsAsync(IIdentity id, int fromEventSequenceNumber, CancellationToken cancellationToken)
        {
            return await MongoDbEventStoreCollection
                .Find(model => model.AggregateId == id.Value && model.AggregateSequenceNumber >= fromEventSequenceNumber)
                .ToListAsync(cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);
        }

        public async Task DeleteEventsAsync(IIdentity id, CancellationToken cancellationToken)
        {
            DeleteResult affectedRows = await MongoDbEventStoreCollection
                .DeleteManyAsync(x => x.AggregateId == id.Value, cancellationToken)
                .ConfigureAwait(continueOnCapturedContext: false);

            _log.Verbose("Deleted entity with ID '{0}' by deleting all of its {1} events", id, affectedRows.DeletedCount);
        }

        private IMongoCollection<MongoDbEventDataModel> MongoDbEventStoreCollection => _mongoDatabase.GetCollection<MongoDbEventDataModel>(CollectionName);
    }
}
