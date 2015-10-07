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
using System.Data.SQLite;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.EventStores.SQLite.Extensions;
using EventFlow.Extensions;
using EventFlow.MetadataProviders;
using EventFlow.Queries;
using EventFlow.ReadStores;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.Aggregates.Test.ReadModels;

namespace EventFlow.EventStores.SQLite.Tests.IntegrationTests
{
    public class SQLiteEventStoreTestConfiguration : IntegrationTestConfiguration
    {
        private IQueryProcessor _queryProcessor;
        private IReadModelPopulator _readModelPopulator;
        private string _databasePath;

        public override IRootResolver CreateRootResolver(IEventFlowOptions eventFlowOptions)
        {
            _databasePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid().ToString("N")}.sqlite");

            SQLiteConnection.CreateFile(_databasePath);
            GC.Collect();

            var resolver = eventFlowOptions
                .UseInMemoryReadStoreFor<InMemoryTestAggregateReadModel>()
                .AddMetadataProvider<AddGuidMetadataProvider>()
                .UseSQLiteEventStore()
                .RegisterServices(sr =>
                    {
                        sr.Register<IConnection, Connection>();
                        sr.Register(r => new SQLiteConnection($"Data Source={_databasePath};Version=3;"));
                    })
                .CreateResolver();

            var connection = resolver.Resolve<IConnection>();
            const string sql = @"
                CREATE TABLE [EventFlow](
	                [GlobalSequenceNumber] [INTEGER] PRIMARY KEY ASC NOT NULL,
	                [BatchId] [uniqueidentifier] NOT NULL,
	                [AggregateId] [nvarchar](255) NOT NULL,
	                [AggregateName] [nvarchar](255) NOT NULL,
	                [Data] [nvarchar](1024) NOT NULL,
	                [Metadata] [nvarchar](1024) NOT NULL,
	                [AggregateSequenceNumber] [int] NOT NULL
                )";
            connection.ExecuteAsync(Label.Named("create-table"), CancellationToken.None, sql, null).Wait();

            _queryProcessor = resolver.Resolve<IQueryProcessor>();
            _readModelPopulator = resolver.Resolve<IReadModelPopulator>();

            return resolver;
        }

        public override async Task<ITestAggregateReadModel> GetTestAggregateReadModelAsync(IIdentity id)
        {
            return await _queryProcessor.ProcessAsync(new ReadModelByIdQuery<InMemoryTestAggregateReadModel>(id.Value), CancellationToken.None).ConfigureAwait(false);
        }

        public override Task PurgeTestAggregateReadModelAsync()
        {
            return _readModelPopulator.PurgeAsync<InMemoryTestAggregateReadModel>(CancellationToken.None);
        }

        public override Task PopulateTestAggregateReadModelAsync()
        {
            return _readModelPopulator.PopulateAsync<InMemoryTestAggregateReadModel>(CancellationToken.None);
        }

        public override void TearDown()
        {
            if (!string.IsNullOrEmpty(_databasePath) &&
                File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }
}