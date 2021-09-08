// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Core;
using EventFlow.MsSql.Connections;
using EventFlow.MsSql.Helpers.Internals;
using EventFlow.MsSql.RetryStrategies;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// ReSharper disable StringLiteralTypo

namespace EventFlow.MsSql.Helpers
{
    public static class MsSqlHelper
    {
        public static async Task<IMsSqlTestContext> CreateContextAsync(
            string name,
            CancellationToken cancellationToken)
        {
            var sqlConnectionStringBuilder = CreateConnectionStringBuilder(name);

            var serviceCollection = new ServiceCollection()
                .AddLogging(b => b.AddConsole(c => c.DisableColors = true))
                .AddTransient<IMsSqlConnection, MsSqlConnection>()
                .AddTransient<IMsSqlErrorRetryStrategy, MsSqlErrorRetryStrategy>()
                .AddTransient<ITransientFaultHandler<IMsSqlErrorRetryStrategy>>()
                .AddTransient<IMsSqlConnectionFactory, MsSqlConnectionFactory>()
                .AddSingleton(MsSqlConfiguration.New
                    .SetConnectionString(sqlConnectionStringBuilder.ConnectionString));

            var serviceProvider = serviceCollection.BuildServiceProvider(true);

            return new MsSqlTestContext(serviceProvider);
        }

        private static SqlConnectionStringBuilder CreateConnectionStringBuilder(string name)
        {
            var databaseName = $"{name}_{DateTimeOffset.Now:yyyy-MM-dd-HH-mm}_{Guid.NewGuid():N}";
            return new SqlConnectionStringBuilder
                {
                    InitialCatalog = databaseName,
                    IntegratedSecurity = false,
                    DataSource = GetEnvironmentVariable("EVENTFLOW_MSSQL_SERVER", "."),
                    UserID = GetEnvironmentVariable("EVENTFLOW_MSSQL_PASS", "sa"),
                    Password = GetEnvironmentVariable("EVENTFLOW_MSSQL_USER", "Password12!"),
                };
        }

        private static string GetEnvironmentVariable(string key, string defaultValue)
        {
            var value = Environment.GetEnvironmentVariable(key);
            return string.IsNullOrEmpty(value)
                ? defaultValue
                : value;
        }
    }
}
