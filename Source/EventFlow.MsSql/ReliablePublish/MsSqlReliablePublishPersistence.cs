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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.EventStores;
using EventFlow.PublishRecovery;
using EventFlow.Subscribers;

namespace EventFlow.MsSql.ReliablePublish
{
    public sealed class MsSqlReliablePublishPersistence : IReliablePublishPersistence
    {
        private readonly IMsSqlConnection _msSqlConnection;

        public MsSqlReliablePublishPersistence(IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task MarkPublishedAsync(IIdentity aggregateIdentity, IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            var item = new PublishLogItem
            {
                AggregateId = aggregateIdentity.Value,
                MinAggregateSequenceNumber = domainEvents.Min(x => x.AggregateSequenceNumber),
                MaxAggregateSequenceNumber = domainEvents.Max(x => x.AggregateSequenceNumber),
            };

            await _msSqlConnection.ExecuteAsync(
                    Label.Named("publishlog-commit"),
                    CancellationToken.None, // Unable to Cancel
                    @"INSERT INTO [dbo].[EventFlowPublishLog]
                    (AggregateId, MinAggregateSequenceNumber, MaxAggregateSequenceNumber)
                    VALUES
                    (@AggregateId, @MinAggregateSequenceNumber, @MaxAggregateSequenceNumber)",
                    item)
                .ConfigureAwait(false);
        }

        public async Task<VerificationState> GetUnverifiedItemsAsync(int maxCount, CancellationToken cancellationToken)
        {
            var logItems = await _msSqlConnection.QueryAsync<PublishLogItem>(
                    Label.Named("publishlog-select"),
                    cancellationToken,
                    "SELECT TOP(@Top) [Id], [AggregateId],[MinAggregateSequenceNumber], [MaxAggregateSequenceNumber] FROM [dbo].[EventFlowPublishLog] ORDER BY [Id]",
                    new { Top = maxCount })
                .ConfigureAwait(false);

            var positions = await _msSqlConnection.QueryAsync<string>(
                    Label.Named("publishlog-global-position-select"),
                    cancellationToken,
                    "SELECT TOP 1 [LastVerifiedPosition] FROM [dbo].[EventFlowPublishVerifyState]")
                .ConfigureAwait(false);

            return new VerificationState(
                new GlobalPosition(positions.First()),
                logItems);
        }

        public async Task MarkVerifiedAsync(
            IReadOnlyCollection<IPublishVerificationItem> verifiedItems,
            GlobalPosition newVerifiedPosition,
            CancellationToken cancellationToken)
        {
            await _msSqlConnection.ExecuteAsync(
                Label.Named("publishlog-global-position-update"),
                cancellationToken,
                "UPDATE [dbo].[EventFlowPublishVerifyState] SET LastVerifiedPosition = @LastVerifiedPosition",
                new
                {
                    LastVerifiedPosition = newVerifiedPosition.Value
                });

            foreach (var publishVerificationItem in verifiedItems)
            {
                var logItem = (PublishLogItem)publishVerificationItem;

                await _msSqlConnection.ExecuteAsync(
                    Label.Named("publishlog-confirm"),
                    cancellationToken,
                    "DELETE FROM [dbo].[EventFlowPublishLog] WHERE Id = @Id",
                    new { logItem.Id});
            }
        }

        private sealed class PublishLogItem : IPublishVerificationItem
        {
            public long Id { get; set; }
            public string AggregateId { get; set; }
            public int MinAggregateSequenceNumber { get; set; }
            public int MaxAggregateSequenceNumber { get; set; }

            public bool IsPublished(ICommittedDomainEvent committedDomainEvent)
            {
                return AggregateId == committedDomainEvent.AggregateId &&
                    MinAggregateSequenceNumber <= committedDomainEvent.AggregateSequenceNumber &&
                       committedDomainEvent.AggregateSequenceNumber <= MaxAggregateSequenceNumber;
            }

            public bool IsFinalEvent(ICommittedDomainEvent committedDomainEvent)
            {
                return committedDomainEvent.AggregateId == AggregateId &&
                       MaxAggregateSequenceNumber == committedDomainEvent.AggregateSequenceNumber;
            }
        }
    }
}