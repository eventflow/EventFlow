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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Queries;
using EventFlow.SQLite.Connections;
using EventFlow.SQLite.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Queries;

namespace EventFlow.SQLite.Tests.IntegrationTests.ReadStores.QueryHandlers
{
    public class SQLiteThingyGetQueryHandler : IQueryHandler<ThingyGetQuery, Thingy>
    {
        private readonly ISQLiteConnection _sqLiteConnection;

        public SQLiteThingyGetQueryHandler(
            ISQLiteConnection sqLiteConnection)
        {
            _sqLiteConnection = sqLiteConnection;
        }

        public async Task<Thingy> ExecuteQueryAsync(ThingyGetQuery query, CancellationToken cancellationToken)
        {
            var readModels = await _sqLiteConnection.QueryAsync<SQLiteThingyReadModel>(
                Label.Named("sqlite-fetch-test-read-model"),
                cancellationToken,
                "SELECT * FROM [ReadModel-ThingyAggregate] WHERE AggregateId = @AggregateId",
                new { AggregateId = query.ThingyId.Value })
                .ConfigureAwait(false);
            return readModels.SingleOrDefault()?.ToThingy();
        }
    }
}