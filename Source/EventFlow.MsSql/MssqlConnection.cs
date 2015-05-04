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
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EventFlow.Core;
using EventFlow.MsSql.Integrations;
using EventFlow.MsSql.RetryStrategies;

namespace EventFlow.MsSql
{
    public class MsSqlConnection : IMsSqlConnection
    {
        private readonly IMsSqlConfiguration _configuration;
        private readonly ITransientFaultHandler _transientFaultHandler;

        public MsSqlConnection(
            IMsSqlConfiguration configuration,
            ITransientFaultHandler transientFaultHandler)
        {
            _configuration = configuration;
            _transientFaultHandler = transientFaultHandler;

            _transientFaultHandler.Use<ISqlErrorRetryStrategy>();
        }

        public Task<int> ExecuteAsync(CancellationToken cancellationToken, string sql, object param = null)
        {
            return WithConnectionAsync(
                (c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.ExecuteAsync(commandDefinition);
                    },
                cancellationToken);
        }

        public async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(CancellationToken cancellationToken, string sql, object param = null)
        {
            return (
                await WithConnectionAsync((c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.QueryAsync<TResult>(commandDefinition);
                    },
                cancellationToken)
                .ConfigureAwait(false))
                .ToList();
        }

        public Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(CancellationToken cancellationToken, string sql, IEnumerable<TRow> rows, object param = null)
            where TRow : class, new()
        {
            var tableParameter = new TableParameter<TRow>("@rows", rows, param ?? new { });
            return QueryAsync<TResult>(cancellationToken, sql, tableParameter);
        }

        public Task<TResult> WithConnectionAsync<TResult>(Func<IDbConnection, CancellationToken, Task<TResult>> withConnection, CancellationToken cancellationToken)
        {
            return _transientFaultHandler.TryAsync(
                async c =>
                    {
                        using (var sqlConnection = new SqlConnection(_configuration.ConnectionString))
                        {
                            await sqlConnection.OpenAsync(c).ConfigureAwait(false);
                            return await withConnection(sqlConnection, c).ConfigureAwait(false);
                        }
                    },
                cancellationToken);
        }
    }
}
