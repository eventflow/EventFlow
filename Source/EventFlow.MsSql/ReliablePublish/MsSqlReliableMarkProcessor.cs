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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Subscribers;

namespace EventFlow.MsSql.ReliablePublish
{
    public sealed class MsSqlReliableMarkProcessor : IReliableMarkProcessor
    {
        private readonly IMsSqlConnection _msSqlConnection;

        public MsSqlReliableMarkProcessor(IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task MarkPublishedWithSuccess(IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            var count = domainEvents.Count;
            if (count == 0)
            {
                return;
            }

            var aggregateIdentity = domainEvents.First().GetIdentity();

            if (domainEvents.Any(x => !Equals(x.GetIdentity(), aggregateIdentity)))
            {
                throw new NotSupportedException("Mark events as published successfully for several aggregates is not supported");
            }

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
    }
}