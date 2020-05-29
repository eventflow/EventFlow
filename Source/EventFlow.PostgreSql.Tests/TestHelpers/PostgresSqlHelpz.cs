// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
// Copyright (c) 2015-2019 eBay Software Foundation
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

namespace EventFlow.PostgreSql.TestsHelpers

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

            var connectionstringParts = new List<string>
            {
                $"Database={databaseName}"
            };

            var environmentServer = Environment.GetEnvironmentVariable("HELPZ_POSTGRESQL_SERVER", EnvironmentVariableTarget.Machine);
            var environmentPort = Environment.GetEnvironmentVariable("HELPZ_POSTGRESQL_PORT", EnvironmentVariableTarget.Machine);
            var environmentPassword = Environment.GetEnvironmentVariable("HELPZ_POSTGRESQL_PASS") ?? "Password12!";
            var envrionmentUsername = Environment.GetEnvironmentVariable("HELPZ_POSTGRESQL_USER", EnvironmentVariableTarget.Machine);

            

            environmentServer = "localhost";
            environmentPort = "5432";
            envrionmentUsername = "postgres";
            
            connectionstringParts.Add(string.IsNullOrEmpty(environmentServer)
                ? @"Server=localhost"
                : $"Server={environmentServer}");
            connectionstringParts.Add(string.IsNullOrEmpty(envrionmentUsername)
                ? @"User Id=postgres"
                : $"User Id={envrionmentUsername}");
            connectionstringParts.Add(string.IsNullOrEmpty(environmentPort)
                ? @"Port=5432"
                : $"Port={environmentPort}");
            if (!string.IsNullOrEmpty(environmentPassword))
            {
                connectionstringParts.Add($"Password={environmentPassword}");
            }

            return new PostgreSqlConnectionString(string.Join(";", connectionstringParts));
        }
    }
}