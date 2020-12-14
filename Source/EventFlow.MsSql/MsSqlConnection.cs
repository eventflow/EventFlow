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

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.MsSql.Connections;
using EventFlow.MsSql.Integrations;
using EventFlow.MsSql.RetryStrategies;
using EventFlow.Sql.Connections;

// ReSharper disable StringLiteralTypo

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
                "Using optimized table type to insert with SQL: {0}",
                sql);
            var tableParameter = new TableParameter<TRow>("@rows", rows, new {});
            return QueryAsync<TResult>(label, cancellationToken, sql, tableParameter);
        }

        private static readonly ConcurrentDictionary<string, Task<IReadOnlyCollection<BulkColumn>>> BulkColumns = new ConcurrentDictionary<string, Task<IReadOnlyCollection<BulkColumn>>>();

        private class BulkColumn
        {
            public string Name { get; }
            public Type Type { get; }
            public Func<object, object> Fetcher { get; }

            public BulkColumn(
                string name,
                Type type,
                Func<object, object> fetcher)
            {
                Name = name;
                Type = type;
                Fetcher = fetcher;
            }
        }

        private class ColumnDataModel
        {
            public string Name { get; set; }
            public bool IsIdentity { get; set; }
            public string TypeName { get; set; }

            public override string ToString()
            {
                return $"Name: {Name}, IsIdentity: {IsIdentity}, Type: {TypeName}";
            }
        }

        private static readonly ConcurrentDictionary<string, Type> DbTypeMap =
            new ConcurrentDictionary<string, Type>(StringComparer.OrdinalIgnoreCase)
            {
                ["bigint"] = typeof(long),
                ["int"] = typeof(int),
                ["uniqueidentifier"] = typeof(Guid)
            };

        private async Task<IReadOnlyCollection<BulkColumn>> GetColumnsAsync(
            Type type,
            string tableName,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                    C.name AS Name,
                    C.is_identity AS IsIdentity,
                    T.name AS TypeName
                FROM
                    sys.columns AS C,
                    sys.types AS T
                WHERE
                    C.object_id = object_id(@TableName) AND
                    C.user_type_id = T.user_type_id
                ORDER BY
                    column_id";

            var properties = type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .ToDictionary(pi => pi.Name, pi => pi, StringComparer.OrdinalIgnoreCase);

            var columns = await QueryAsync<ColumnDataModel>(
                Label.Named("fetch-columns-information"),
                cancellationToken,
                sql,
                new {TableName = tableName})
                .ConfigureAwait(false);

            var list = new List<BulkColumn>();

            foreach (var column in columns)
            {
                if (column.IsIdentity)
                {
                    if (!DbTypeMap.TryGetValue(column.TypeName, out var t))
                    {
                        throw new InvalidOperationException(
                            $"Currently DB type {column.TypeName} isn't supported as identity");
                    }

                    list.Add(new BulkColumn(column.Name, t, o => DBNull.Value));
                    continue;
                }

                if (!properties.TryGetValue(column.Name, out var pi))
                {
                    throw new InvalidOperationException(
                        $"Type '{type.Name}' is missing property '{column.Name}' which is in the table '{tableName}'");
                }

                list.Add(new BulkColumn(column.Name, pi.PropertyType, pi.GetValue));
            }

            return list;
        }

        public async Task<long> BulkCopyAsync<T>(
            Label label,
            string tableName,
            Func<CancellationToken, Task<IReadOnlyCollection<T>>> factory,
            CancellationToken cancellationToken)
        {
            var type = typeof(T);
            var columns = await BulkColumns.GetOrAdd(
                $"{tableName}-{type.FullName}",
                _ => GetColumnsAsync(type, tableName, cancellationToken))
                .ConfigureAwait(false);

            var stopwatch = Stopwatch.StartNew();
            var rows = await WithConnectionAsync(
                label,
                async (c, ct) =>
                    {
                        if (!(c is SqlConnection sqlConnection))
                        {
                            throw new InvalidOperationException("Only works for MSSQL connections");
                        }

                        IReadOnlyCollection<T> bulk;
                        var i = 0L;

                        while (((bulk = await factory(ct).ConfigureAwait(false))?.Any()).GetValueOrDefault())
                        {
                            if (bulk == null || !bulk.Any())
                            {
                                break;
                            }

                            using (var sqlBulkCopy = new SqlBulkCopy(sqlConnection))
                            {
                                sqlBulkCopy.BatchSize = bulk.Count;
                                sqlBulkCopy.DestinationTableName = tableName;
                                var dataTable = new DataTable(tableName);

                                foreach (var column in columns)
                                {
                                    dataTable.Columns.Add(column.Name, column.Type);
                                }

                                foreach (var row in bulk)
                                {
                                    var values = columns
                                        .Select(cl => cl.Fetcher(row))
                                        .ToArray();
                                    dataTable.Rows.Add(values);
                                    i++;
                                }

                                await sqlBulkCopy.WriteToServerAsync(
                                        dataTable,
                                        cancellationToken)
                                    .ConfigureAwait(false);
                            }
                        }

                        return i;
                    },
                cancellationToken)
                .ConfigureAwait(false);

            Log.Debug($"Bulk inserted {rows} in {stopwatch.Elapsed.TotalSeconds:0.##} seconds");

            return rows;
        }
    }
}
