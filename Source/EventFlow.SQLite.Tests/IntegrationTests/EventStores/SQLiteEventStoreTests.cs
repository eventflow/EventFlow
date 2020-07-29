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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.SQLite.Connections;
using EventFlow.SQLite.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.SQLite.Tests.IntegrationTests.EventStores
{
    [Category(Categories.Integration)]
    public class SQLiteEventStoreTests : TestSuiteForEventStore
    {
        private string _databasePath;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");

            using (File.Create(_databasePath)){ }

            var resolver = eventFlowOptions
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .ConfigureSQLite(SQLiteConfiguration.New.SetConnectionString($"Data Source={_databasePath};Version=3;"))
                .UseSQLiteEventStore()
                .CreateResolver();

            var connection = resolver.Resolve<ISQLiteConnection>();
            const string sqlCreateTable = @"
                CREATE TABLE [EventFlow](
                    [GlobalSequenceNumber] [INTEGER] PRIMARY KEY ASC NOT NULL,
                    [BatchId] [uniqueidentifier] NOT NULL,
                    [AggregateId] [nvarchar](255) NOT NULL,
                    [AggregateName] [nvarchar](255) NOT NULL,
                    [Data] [nvarchar](1024) NOT NULL,
                    [Metadata] [nvarchar](1024) NOT NULL,
                    [AggregateSequenceNumber] [int] NOT NULL
                )";
            const string sqlCreateIndex = @"
                CREATE UNIQUE INDEX [IX_EventFlow_AggregateId_AggregateSequenceNumber] ON [EventFlow]
                (
                    [AggregateId] ASC,
                    [AggregateSequenceNumber] ASC
                )";
            connection.ExecuteAsync(Label.Named("create-table"), CancellationToken.None, sqlCreateTable, null).Wait();
            connection.ExecuteAsync(Label.Named("create-index"), CancellationToken.None, sqlCreateIndex, null).Wait();

            return resolver;
        }

        [TearDown]
        public void TearDown()
        {
            if (!string.IsNullOrEmpty(_databasePath) &&
                File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }
}