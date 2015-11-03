// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.MsSql.Tests.ReadModels;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Test.Entities;
using EventFlow.TestHelpers.Aggregates.Test.Queries;

namespace EventFlow.MsSql.Tests.IntegrationTests.QueryHandlers
{
    public class MsSqlGetItemsQueryHandler : IQueryHandler<GetItemsQuery, IReadOnlyCollection<TestItem>>
    {
        private readonly IMsSqlConnection _msSqlConnection;

        public MsSqlGetItemsQueryHandler(
            IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task<IReadOnlyCollection<TestItem>> ExecuteQueryAsync(GetItemsQuery query, CancellationToken cancellationToken)
        {
            // TODO: Store the aggrgate that it belongs to
            // TODO: Fix bad naming for AggregateId column

            var readModels = await _msSqlConnection.QueryAsync<MsSqlTestAggregateItemReadModel>(
                Label.Named("fetch"),
                cancellationToken,
                "SELECT * FROM [ReadModel-TestAggregateItem]")
                .ConfigureAwait(false);

            return readModels
                .Select(rm => new TestItem(TestItemId.With(rm.AggregateId)))
                .ToList();
        }
    }
}