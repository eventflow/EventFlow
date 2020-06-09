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
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.Subscribers;

namespace EventFlow.MsSql.ReliablePublish
{
    public sealed class MsSqlPublishVerificator : IPublishVerificator
    {
        private const int PageSize = 200;

        private readonly IEventPersistence _eventPersistence;
        private readonly IPublishRecoveryProcessor _publishRecoveryProcessor;
        private readonly IMsSqlConnection _msSqlConnection;
        private readonly IEventJsonSerializer _eventSerializer;
        private readonly IRecoveryDetector _recoveryDetector;

        public MsSqlPublishVerificator(IEventPersistence eventPersistence, IMsSqlConnection msSqlConnection, IPublishRecoveryProcessor publishRecoveryProcessor, IEventJsonSerializer eventSerializer, IRecoveryDetector recoveryDetector)
        {
            _eventPersistence = eventPersistence;
            _msSqlConnection = msSqlConnection;
            _publishRecoveryProcessor = publishRecoveryProcessor;
            _eventSerializer = eventSerializer;
            _recoveryDetector = recoveryDetector;
        }

        public TimeSpan ReliableThreshold { get; set; } = TimeSpan.FromMinutes(5);

        public Task<PublishVerificationResult> VerifyOnceAsync(CancellationToken cancellationToken)
        {
            return _msSqlConnection.WithConnectionAsync(
                        Label.Named("publishlog-verify"),
                        VerifyAsync,
                        cancellationToken);
        }

        private async Task<PublishVerificationResult> VerifyAsync(IDbConnection db, CancellationToken cancellationToken)
        {
            var result = PublishVerificationResult.CompletedNoMoreDataToVerify;

            VerifyResult verifyResult;
            AllCommittedEventsPage page;
            using (var transaction = db.BeginTransaction())
            {
                var state = await GetPreviousVerifyStateAndLockItAsync(db, transaction).ConfigureAwait(false);
                var position = new GlobalPosition(state.LastVerifiedPosition);

                var logItemLookup = await GetLogItemsAsync(db, transaction);

                page = await _eventPersistence.LoadAllCommittedEvents(position, PageSize, cancellationToken)
                    .ConfigureAwait(false);
                state.LastVerifiedPosition = page.NextGlobalPosition.Value;

                verifyResult = VerifyDomainEvents(page, logItemLookup);

                // Some of not published events can be in flight, so no need recovery them
                // but we have to check them again on next iteration
                var eventsForRecovery = GetEventsForRecovery(verifyResult.UnpublishedEvents);

                if (eventsForRecovery.Count > 0)
                {
                    // Do it inside transaction to recover in single thread
                    // success recovery should put LogItem
                    await _publishRecoveryProcessor.RecoverEventsAsync(eventsForRecovery, cancellationToken)
                        .ConfigureAwait(false);

                    result = PublishVerificationResult.RecoveredNeedVerify;
                }

                // Remove logs and move position forward only when it is successfully recovered.
                if (verifyResult.UnpublishedEvents.Count == 0)
                {
                    await RemoveLogItemsAsync(verifyResult.PublishedLogItems, db, transaction)
                        .ConfigureAwait(false);

                    await UpdateLastVerifyStateAsync(db, state, transaction)
                        .ConfigureAwait(false);

                    result = page.CommittedDomainEvents.Count < PageSize
                        ? PublishVerificationResult.CompletedNoMoreDataToVerify
                        : PublishVerificationResult.HasMoreDataNeedVerify;
                }

                transaction.Commit();
            }

            return result;
        }

        private IReadOnlyList<IDomainEvent> GetEventsForRecovery(IReadOnlyList<ICommittedDomainEvent> unpublishedEvents)
        {
            return unpublishedEvents
                .Select(evnt => _eventSerializer.Deserialize(evnt))
                .Where(evnt => _recoveryDetector.IsNeedRecovery(evnt))
                .ToList();
        }

        private async Task RemoveLogItemsAsync(IReadOnlyList<PublishLogItem> publishedLogItems, IDbConnection db, IDbTransaction transaction)
        {
            foreach (var logItem in publishedLogItems)
            {
                await db.ExecuteAsync("DELETE FROM [dbo].[EventFlowPublishLog] WHERE Id = @Id", logItem, transaction);
            }
        }

        private VerifyResult VerifyDomainEvents(AllCommittedEventsPage page, ILookup<string, PublishLogItem> logItemLookup)
        {
            var unpublishedEvents = new List<ICommittedDomainEvent>();
            var publishedLogItems = new List<PublishLogItem>();

            foreach (var committedDomainEvent in page.CommittedDomainEvents)
            {
                var logItem = TryGetPublishedLogItem(committedDomainEvent, logItemLookup);

                if (logItem == null)
                {
                    unpublishedEvents.Add(committedDomainEvent);
                }
                // Remove logItem only on the last event related with this log item
                else if (committedDomainEvent.AggregateSequenceNumber == logItem.MaxAggregateSequenceNumber)
                {
                    publishedLogItems.Add(logItem);
                }
            }

            return new VerifyResult(unpublishedEvents, publishedLogItems);
        }

        private static async Task<ILookup<string, PublishLogItem>> GetLogItemsAsync(IDbConnection db, IDbTransaction transaction)
        {
            var logItems = await db.QueryAsync<PublishLogItem>(
                    "SELECT [Id], [AggregateId],[MinAggregateSequenceNumber], [MaxAggregateSequenceNumber] FROM [dbo].[EventFlowPublishLog]",
                    transaction: transaction)
                .ConfigureAwait(false);

            return logItems.ToLookup(x => x.AggregateId);
        }

        private static Task UpdateLastVerifyStateAsync(IDbConnection db, VerifyState state,
            IDbTransaction transaction)
        {
            return db.ExecuteAsync(
                    "UPDATE [dbo].[EventFlowPublishVerifyState] SET LastVerifiedPosition = @LastVerifiedPosition WHERE Id = @Id",
                    state,
                    transaction);
        }

        private static async Task<VerifyState> GetPreviousVerifyStateAndLockItAsync(IDbConnection db, IDbTransaction transaction)
        {
            var state = await db.QueryFirstOrDefaultAsync<VerifyState>(
                    "SELECT [Id], [LastVerifiedPosition] FROM [dbo].[EventFlowPublishVerifyState] WITH (XLOCK)", transaction: transaction)
                .ConfigureAwait(false);

            return state;
        }

        private PublishLogItem TryGetPublishedLogItem(ICommittedDomainEvent committedDomainEvent, ILookup<string, PublishLogItem> logItemLookup)
        {
            var logItems = logItemLookup[committedDomainEvent.AggregateId];

            return logItems.FirstOrDefault(
                logItem => logItem.MinAggregateSequenceNumber <= committedDomainEvent.AggregateSequenceNumber &&
                           committedDomainEvent.AggregateSequenceNumber <= logItem.MaxAggregateSequenceNumber);
        }

        public sealed class VerifyState
        {
            public long Id { get; set; }
            public string LastVerifiedPosition { get; set; }
        }

        private sealed class VerifyResult
        {
            public VerifyResult(
                IReadOnlyList<ICommittedDomainEvent> unpublishedEvents,
                IReadOnlyList<PublishLogItem> publishedLogItems)
            {
                UnpublishedEvents = unpublishedEvents;
                PublishedLogItems = publishedLogItems;
            }

            public IReadOnlyList<ICommittedDomainEvent> UnpublishedEvents { get; }

            public IReadOnlyList<PublishLogItem> PublishedLogItems { get; }
        }
    }
}