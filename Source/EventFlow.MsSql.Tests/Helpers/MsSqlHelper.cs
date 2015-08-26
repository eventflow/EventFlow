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
using System.Data.SqlClient;
using EventFlow.MsSql.Tests.Extensions;

namespace EventFlow.MsSql.Tests.Helpers
{
    public static class MsSqlHelper
    {
        public static ITestDatabase CreateDatabase(string partialDatabaseName)
        {
            var connectionstring = GetConnectionstring(partialDatabaseName);
            var masterConnectionstring = connectionstring.ReplaceDatabaseInConnectionstring("master");
            var testDatabaseName = connectionstring.GetDatabaseInConnectionstring();

            using (var sqlConnection = new SqlConnection(masterConnectionstring))
            {
                sqlConnection.Open();
                Console.WriteLine("MssqlHelper: Creating database '{0}'", testDatabaseName);
                var sql = $"CREATE DATABASE [{testDatabaseName}]";
                using (var sqlCommand = new SqlCommand(sql, sqlConnection))
                {
                    sqlCommand.ExecuteNonQuery();
                }
            }

            return new TestDatabase(connectionstring);
        }

        public static string GetConnectionstring(string partialDatabaseName)
        {
            var databaseName = string.Format(
                "Test_{0}_{1}_{2}",
                partialDatabaseName,
                DateTime.Now.ToString("yyyy-MM-dd-HH-mm"),
                Guid.NewGuid());

            var connectionstringParts = new List<string>
                {
                    $"Database={databaseName}",
                };

            var environmentServer = Environment.GetEnvironmentVariable("MSSQL_SERVER");
            var environmentPassword = Environment.GetEnvironmentVariable("MSSQL_PASS");
            var envrionmentUsername = Environment.GetEnvironmentVariable("MSSQL_USER");

            connectionstringParts.Add(string.IsNullOrEmpty(environmentServer)
                ? @"Server=localhost\SQLEXPRESS"
                : $"Server={environmentServer}");
            connectionstringParts.Add(string.IsNullOrEmpty(envrionmentUsername)
                ? @"Integrated Security=True"
                : $"User Id={envrionmentUsername}");
            if (!string.IsNullOrEmpty(environmentPassword))
            {
                connectionstringParts.Add($"Password={environmentPassword}");
            }

            return string.Join(";", connectionstringParts);
        }
    }
}
