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

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.MsSql;
using EventFlow.Queries;

namespace EventFlow.ReadStores.MsSql.Queries
{
    public class MsSqlReadModelByIdQueryHandler<TReadModel> : IQueryHandler<ReadModelByIdQuery<TReadModel>, TReadModel>
        where TReadModel : IMssqlReadModel
    {
        private readonly IReadModelSqlGenerator _readModelSqlGenerator;
        private readonly IMsSqlConnection _connection;

        public MsSqlReadModelByIdQueryHandler(
            IReadModelSqlGenerator readModelSqlGenerator,
            IMsSqlConnection connection)
        {
            _readModelSqlGenerator = readModelSqlGenerator;
            _connection = connection;
        }

        public async Task<TReadModel> ExecuteQueryAsync(ReadModelByIdQuery<TReadModel> query, CancellationToken cancellationToken)
        {
            var readModelNameLowerCased = typeof(TReadModel).Name.ToLowerInvariant();
            var selectSql = _readModelSqlGenerator.CreateSelectSql<TReadModel>();
            var readModels = await _connection.QueryAsync<TReadModel>(
                Label.Named(string.Format("mssql-fetch-read-model-{0}", readModelNameLowerCased)),
                cancellationToken,
                selectSql,
                new { AggregateId = query.Id })
                .ConfigureAwait(false);
            return readModels.SingleOrDefault();            
        }
    }
}
