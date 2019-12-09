// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rida Messaoudene
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
using EventFlow.PostgreSql.TestsHelpers;

namespace EventFlow.PostgreSql.Tests.TestHelpers
{
    public static class PostgreSqlHelpz
    {
        public static IPostgreSqlDatabase CreateDatabase(string label, bool dropOnDispose = true)
        {
            var connectionString = CreateConnectionString(label);
            var masterConnectionString = connectionString.NewConnectionString("postgres");

            var sql = $"CREATE DATABASE \"{connectionString.Database}\";";
            masterConnectionString.Execute(sql);

            return new PostgressSqlDatabase(connectionString, dropOnDispose);
        }

        public static PostgreSqlConnectionString CreateConnectionString(string label)
        {
            var databaseName = $"{label}_{DateTime.Now:yyyy-MM-dd-HH-mm}_{Guid.NewGuid():N}";

            var connectionStringParts = new List<string>
                {
                    $"Database={databaseName}"
                };

            var server = GetEnvironmentVariableOrDefault("EVENTFLOW_POSTGRESQL_SERVER", "localhost");
            var port = GetEnvironmentVariableOrDefault("EVENTFLOW_POSTGRESQL_PORT", "5432");
            var password = GetEnvironmentVariableOrDefault("EVENTFLOW_POSTGRESQL_PASS", "postgres");
            var username = GetEnvironmentVariableOrDefault("EVENTFLOW_POSTGRESQL_USER", "Password12!");

            connectionStringParts.Add(string.IsNullOrEmpty(server)
                ? @"Server=localhost"
                : $"Server={server}");
            connectionStringParts.Add(string.IsNullOrEmpty(username)
                ? @"User Id=postgres"
                : $"User Id={username}");
            connectionStringParts.Add(string.IsNullOrEmpty(port)
                ? @"Port=5432"
                : $"Port={port}");

            if (!string.IsNullOrEmpty(password))
            {
                connectionStringParts.Add($"Password={password}");
            }

            return new PostgreSqlConnectionString(string.Join(";", connectionStringParts));
        }

        private static string GetEnvironmentVariableOrDefault(
            string key,
            string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }
    }
}
