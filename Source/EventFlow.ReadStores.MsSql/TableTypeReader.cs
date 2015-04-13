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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.MsSql;

namespace EventFlow.ReadStores.MsSql
{
    public class TableTypeReader : ITableTypeReader
    {
        private readonly IMsSqlConnection _msSqlConnection;

        public class Column
        {
            public string Name { get; set; }
            public string DataType { get; set; }
            public string IsNullable { get; set; }
            public int? MaxLength { get; set; }
            public int? DateTimePrecision { get; set; }
        }

        public TableTypeReader(
            IMsSqlConnection msSqlConnection)
        {
            _msSqlConnection = msSqlConnection;
        }

        public async Task<IReadOnlyCollection<ColumnDescription>> GetColumnDescriptionsAsync(
            string tableName,
            CancellationToken cancellationToken)
        {
            const string sql = @"
                SELECT
                    COLUMN_NAME AS Name,
                    DATA_TYPE AS DataType,
                    IS_NULLABLE AS IsNullable,
                    CHARACTER_MAXIMUM_LENGTH AS MaxLength,
                    DATETIME_PRECISION AS DateTimePrecision
                FROM INFORMATION_SCHEMA.COLUMNS
                WHERE TABLE_NAME = @TableName
                ORDER BY COLUMN_NAME";

            var columns = await _msSqlConnection.QueryAsync<Column>(
                cancellationToken,
                sql,
                new {TableName = tableName})
                .ConfigureAwait(false);

            return columns
                .Select(c => new ColumnDescription(
                    c.Name,
                    GetSqlDbType(c.DataType),
                    GetIsNullable(c.IsNullable),
                    c.MaxLength,
                    c.DateTimePrecision))
                .ToList();
        }

        private static bool GetIsNullable(string strIsNullable)
        {
            return !strIsNullable.ToLowerInvariant().Equals("no");
        }

        private static SqlDbType GetSqlDbType(string typeName)
        {
            switch (typeName)
            {
                case "int": return SqlDbType.Int;
                case "nvarchar": return SqlDbType.NVarChar;
                case "bigint": return SqlDbType.BigInt;
                case "bit": return SqlDbType.Bit;
                case "datetimeoffset": return SqlDbType.DateTimeOffset;
                default:
                    throw new ArgumentOutOfRangeException(
                        "typeName",
                        string.Format("Unknown column type '{0}'", typeName));
            }
        }
    }
}
