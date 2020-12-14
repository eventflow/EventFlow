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
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.SQLite.Connections;
using EventFlow.SQLite.Extensions;
using EventFlow.SQLite.Tests.IntegrationTests.ReadStores.QueryHandlers;
using EventFlow.SQLite.Tests.IntegrationTests.ReadStores.ReadModels;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Suites;
using NUnit.Framework;

namespace EventFlow.SQLite.Tests.IntegrationTests.ReadStores
{
    [Category(Categories.Integration)]
    public class SQLiteReadStoreTests : TestSuiteForReadModelStore
    {
        protected override Type ReadModelType { get; } = typeof(SQLiteThingyReadModel);

        private string _databasePath;

        protected override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.sqlite");

            using (File.Create(_databasePath)) { }

            var resolver = eventFlowOptions
                .RegisterServices(sr => sr.RegisterType(typeof(ThingyMessageLocator)))
                .ConfigureSQLite(SQLiteConfiguration.New.SetConnectionString($"Data Source={_databasePath};Version=3;"))
                .UseSQLiteReadModel<SQLiteThingyReadModel>()
                .UseSQLiteReadModel<SQLiteThingyMessageReadModel, ThingyMessageLocator>()
                .AddQueryHandlers(
                    typeof(SQLiteThingyGetQueryHandler),
                    typeof(SQLiteThingyGetVersionQueryHandler),
                    typeof(SQLiteThingyGetMessagesQueryHandler))
                .CreateResolver();

            var connection = resolver.Resolve<ISQLiteConnection>();
            const string sqlThingyAggregate = @"
                CREATE TABLE [ReadModel-ThingyAggregate](
                    [Id] [INTEGER] PRIMARY KEY ASC,
                    [AggregateId] [nvarchar](64) NOT NULL,
                    [Version] INTEGER,
                    [PingsReceived] [int] NOT NULL,
                    [DomainErrorAfterFirstReceived] [bit] NOT NULL
                )";
            const string sqlThingyMessage = @"
                CREATE TABLE [ReadModel-ThingyMessage](
                    [Id] [INTEGER] PRIMARY KEY ASC,
                    [ThingyId] [nvarchar](64) NOT NULL,
                    [Version] INTEGER,
                    [MessageId] [nvarchar](64) NOT NULL,
                    [Message] [nvarchar](512) NOT NULL
                )";
            connection.ExecuteAsync(Label.Named("create-table"), CancellationToken.None, sqlThingyAggregate, null).Wait();
            connection.ExecuteAsync(Label.Named("create-table"), CancellationToken.None, sqlThingyMessage, null).Wait();

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