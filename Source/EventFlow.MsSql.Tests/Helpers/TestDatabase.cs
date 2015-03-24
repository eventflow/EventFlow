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
using System.Data.SqlClient;
using EventFlow.MsSql.Tests.Extensions;

namespace EventFlow.MsSql.Tests.Helpers
{
    public interface ITestDatabase : IDisposable
    {
        string ConnectionString { get; }
        void Execute(string sql);
    }

    public class TestDatabase : ITestDatabase
    {
        public string ConnectionString { get; private set; }
        public SqlConnection SqlConnection { get; private set; }
        public string Name { get; private set; }

        public TestDatabase(string connectionString)
        {
            ConnectionString = connectionString;
            Name = connectionString.GetDatabaseInConnectionstring();

            SqlConnection = new SqlConnection(ConnectionString);
            SqlConnection.Open();
        }

        public void Execute(string sql)
        {
            using (var sqlCommand = new SqlCommand(sql, SqlConnection))
            {
                sqlCommand.ExecuteNonQuery();
            }
        }

        public void Dispose()
        {
            Console.WriteLine("MssqlHelper: Deleting test database '{0}'", Name);

            var masterConnectionString = ConnectionString.ReplaceDatabaseInConnectionstring("master");
            var sql = string.Format(
                "ALTER DATABASE [{0}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;DROP DATABASE [{0}];",
                Name);

            using (var sqlConnection = new SqlConnection(masterConnectionString))
            {
                sqlConnection.Open();
                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }

            SqlConnection.Dispose();
        }
    }
}
