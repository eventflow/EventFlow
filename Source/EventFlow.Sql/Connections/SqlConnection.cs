// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
//

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EventFlow.Core;
using EventFlow.Sql.Integrations;

namespace EventFlow.Sql.Connections
{
    public abstract class SqlConnection<TConfiguration, TRetryStrategy> : ISqlConnection
        where TConfiguration : ISqlConfiguration
        where TRetryStrategy : IRetryStrategy
    {
        private readonly TConfiguration _configuration;
        private readonly ITransientFaultHandler<TRetryStrategy> _transientFaultHandler;

        protected SqlConnection(
            TConfiguration configuration,
            ITransientFaultHandler<TRetryStrategy> transientFaultHandler)
        {
            _configuration = configuration;
            _transientFaultHandler = transientFaultHandler;
        }

        public virtual Task<int> ExecuteAsync(
            Label label,
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return WithConnectionAsync(
                label,
                (c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.ExecuteAsync(commandDefinition);
                    },
                cancellationToken);
        }

        public virtual async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
            Label label,
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return (
                await WithConnectionAsync(
                label,
                (c, ct) =>
                    {
                        var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                        return c.QueryAsync<TResult>(commandDefinition);
                    },
                cancellationToken)
                .ConfigureAwait(false))
                .ToList();
        }

        public virtual Task<IReadOnlyCollection<TResult>> InsertMultipleAsync<TResult, TRow>(
            Label label,
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows,
            object param = null)
            where TRow : class, new()
        {
            var tableParameter = new TableParameter<TRow>("@rows", rows, param ?? new { });
            return QueryAsync<TResult>(label, cancellationToken, sql, tableParameter);
        }

        public virtual Task<TResult> WithConnectionAsync<TResult>(
            Label label,
            Func<IDbConnection, CancellationToken, Task<TResult>> withConnection,
            CancellationToken cancellationToken)
        {
            return _transientFaultHandler.TryAsync(
                async c =>
                    {
                        using (var sqlConnection = new System.Data.SqlClient.SqlConnection(_configuration.ConnectionString))
                        {
                            await sqlConnection.OpenAsync(c).ConfigureAwait(false);
                            return await withConnection(sqlConnection, c).ConfigureAwait(false);
                        }
                    },
                label,
                cancellationToken);
        }
    }
}