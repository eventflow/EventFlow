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
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using EventFlow.ValueObjects;

namespace EventFlow.TestHelpers.MsSql
{
    public class MsSqlConnectionString : SingleValueObject<string>
    {
        private static readonly Regex DatabaseReplace = new Regex(
            @"(?<key>Initial Catalog|Database)=[a-zA-Z0-9\-_]+",
            RegexOptions.Compiled);
        private static readonly Regex DatabaseExtract = new Regex(
            @"(Initial Catalog|Database)=(?<database>[a-zA-Z0-9\-_]+)",
            RegexOptions.Compiled);
        public MsSqlConnectionString(string value) : base(value)
        {
            if (string.IsNullOrEmpty(value)) throw new ArgumentNullException(nameof(value));
            var match = DatabaseExtract.Match(value);
            if (!match.Success)
            {
                throw new ArgumentException($"Cannot find database name in '{value}'");
            }
            Database = match.Groups["database"].Value;
        }
        public string Database { get; }
        public MsSqlConnectionString NewConnectionString(string toDatabase)
        {
            return new MsSqlConnectionString(DatabaseReplace.Replace(Value, $"${{key}}={toDatabase}"));
        }
        public void Ping()
        {
            Execute("SELECT 1");
        }
        public T WithConnection<T>(Func<SqlConnection, T> action)
        {
            using (var sqlConnection = new SqlConnection(Value))
            {
                sqlConnection.Open();
                return action(sqlConnection);
            }
        }
        public void Execute(string sql)
        {
            Console.WriteLine($"Executing SQL: {sql}");
            WithConnection(c =>
            {
                using (var sqlCommand = new SqlCommand(sql, c))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            });
        }

        public void WithConnection(Action<SqlConnection> action)
        {
            WithConnection(c =>
            {
                action(c);
                return 0;
            });
        }
    }
}