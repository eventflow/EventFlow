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
using System.Linq;

namespace EventFlow.TestHelpers.MsSql
{
    public static class MsSqlHelpz
    {
        private static bool? useIntegratedSecurity;

        public static IMsSqlDatabase CreateDatabase(string label, bool dropOnDispose = true)
        {
            var connectionString = CreateConnectionString(label);
            var masterConnectionString = connectionString.NewConnectionString("master");

            var sql = $"CREATE DATABASE [{connectionString.Database}]";
            masterConnectionString.Execute(sql);

            return new MsSqlDatabase(connectionString, dropOnDispose);
        }

        public static MsSqlConnectionString CreateConnectionString(string label)
        {
            var databaseName = $"{label}_{DateTime.Now:yyyy-MM-dd-HH-mm}_{Guid.NewGuid():N}";

            var connectionStringBuilder = new SqlConnectionStringBuilder()
                {
                    DataSource = FirstNonEmpty(
                        Environment.GetEnvironmentVariable("EVENTFLOW_MSSQL_SERVER"),
                        ".")
                };

            var password = Environment.GetEnvironmentVariable("EVENTFLOW_MSSQL_PASS");
            var username = Environment.GetEnvironmentVariable("EVENTFLOW_MSSQL_USER");

            if (!string.IsNullOrEmpty(username) &&
                !string.IsNullOrEmpty(password))
            {
                connectionStringBuilder.UserID = username;
                connectionStringBuilder.Password = password;
            }
            else
            {
                connectionStringBuilder.IntegratedSecurity = true;

                // Try to use default sql login/password specified in docker-compose.local.yml - for running integration tests locally
                // without locally installed MS SQL server
                if (useIntegratedSecurity == null)
                {
                    useIntegratedSecurity = IsGoodConnectionString(connectionStringBuilder.ConnectionString);
                }

                if (!useIntegratedSecurity.Value)
                {
                    connectionStringBuilder.IntegratedSecurity = false;
                    connectionStringBuilder.UserID = "sa";
                    connectionStringBuilder.Password = "Password12!";
                }
            }

            connectionStringBuilder.InitialCatalog = databaseName;

            Console.WriteLine($"Using connection string for tests: {connectionStringBuilder.ConnectionString}");

            return new MsSqlConnectionString(connectionStringBuilder.ConnectionString);
        }

        private static bool IsGoodConnectionString(string connectionString)
        {
            try
            {
                using (var db = new SqlConnection(connectionString))
                {
                    db.Open();
                }

                return true;
            }
            catch (SqlException)
            {
                return false;
            }
        }

        private static string FirstNonEmpty(params string[] parts)
        {
            return parts.First(s => !string.IsNullOrEmpty(s));
        }
    }
}