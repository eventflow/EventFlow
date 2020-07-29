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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.MsSql.Connections;
using EventFlow.MsSql.Integrations;
using EventFlow.MsSql.RetryStrategies;
using EventFlow.Sql.Connections;

namespace EventFlow.MsSql
{
    public class MsSqlConnection : SqlConnection<IMsSqlConfiguration, IMsSqlErrorRetryStrategy, IMsSqlConnectionFactory>, IMsSqlConnection
    {
        public MsSqlConnection(
            ILog log,
            IMsSqlConfiguration configuration,
            IMsSqlConnectionFactory connectionFactory,
            ITransientFaultHandler<IMsSqlErrorRetryStrategy> transientFaultHandler)
            : base(log, configuration, connectionFactory, transientFaultHandler)
        {
        }

        public override Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(
            Label label,
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows)
        {
            Log.Verbose(
                "Using optimised table type to insert with SQL: {0}",
                sql);
            var tableParameter = new TableParameter<TRow>("@rows", rows, new {});
            return QueryAsync<TResult>(label, cancellationToken, sql, tableParameter);
        }
    }
}