// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
using EventFlow.Core;
using Microsoft.Extensions.Logging;

namespace EventFlow.Sql.Connections
{
    public abstract class SqlConnection<TConfiguration, TRetryStrategy, TConnectionFactory> : ISqlConnection
        where TConfiguration : ISqlConfiguration<TConfiguration>
        where TRetryStrategy : IRetryStrategy
        where TConnectionFactory : ISqlConnectionFactory
    {
        protected SqlConnection(
            ILogger logger,
            TConfiguration configuration,
            TConnectionFactory connectionFactory,
            ITransientFaultHandler<TRetryStrategy> transientFaultHandler)
        {
            Logger = logger;
            ConnectionFactory = connectionFactory;
            Configuration = configuration;
            TransientFaultHandler = transientFaultHandler;
        }

        public ILogger Logger { get; }
        protected TConnectionFactory ConnectionFactory { get; }
        protected TConfiguration Configuration { get; }
        protected ITransientFaultHandler<TRetryStrategy> TransientFaultHandler { get; }

        public virtual Task<int> ExecuteAsync(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return WithConnectionAsync(
                label,
                connectionStringName,
                (c, ct) =>
                {
                    var commandDefinition = new CommandDefinition(sql, param, cancellationToken: ct);
                    return c.ExecuteAsync(commandDefinition);
                },
                cancellationToken);
        }

        public virtual async Task<IReadOnlyCollection<TResult>> QueryAsync<TResult>(
            Label label,
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            object param = null)
        {
            return (
                await WithConnectionAsync(
                    label,
                    connectionStringName,
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
            string connectionStringName,
            CancellationToken cancellationToken,
            string sql,
            IEnumerable<TRow> rows)
            where TRow : class
        {
            Logger.LogDebug("Insert multiple rows non-optimized, inserting one row at a time using SQL {SQL}", sql);

            return WithConnectionAsync<IReadOnlyCollection<TResult>>(
                label,
                connectionStringName,
                async (c, ct) =>
                {
                    using (var transaction = c.BeginTransaction())
                    {
                        try
                        {
                            var results = new List<TResult>();
                            foreach (var row in rows)
                            {
                                var commandDefinition = new CommandDefinition(sql, row, cancellationToken: ct);
                                var result = await c.QueryAsync<TResult>(commandDefinition).ConfigureAwait(false);
                                results.Add(result.First());
                            }
                            transaction.Commit();
                            return results;
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                },
                cancellationToken);
        }

        public virtual Task<TResult> WithConnectionAsync<TResult>(
            Label label,
            string connectionStringName,
            Func<IDbConnection, CancellationToken, Task<TResult>> withConnection,
            CancellationToken cancellationToken)
        {
            return TransientFaultHandler.TryAsync(
                async c =>
                {
                    var connectionString = await Configuration.GetConnectionStringAsync(
                        label,
                        connectionStringName,
                        cancellationToken).ConfigureAwait(false);

                    using (var sqlConnection = await ConnectionFactory.OpenConnectionAsync(connectionString, cancellationToken).ConfigureAwait(false))
                    {
                        return await withConnection(sqlConnection, c).ConfigureAwait(false);
                    }
                },
                label,
                cancellationToken);
        }
    }
}
