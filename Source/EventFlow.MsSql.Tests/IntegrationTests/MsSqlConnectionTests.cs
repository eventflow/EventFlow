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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.MsSql.Extensions;
using EventFlow.Configuration;
using EventFlow.Core;
using EventFlow.MsSql.Tests.Extensions;
using EventFlow.Sql.Migrations;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class MsSqlConnectionTests
    {
        private IMsSqlDatabase _testDatabase;
        private IRootResolver _resolver;
        private IMsSqlConnection _msSqlConnection;

        [SetUp]
        public void SetUp()
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-mssql");
            _resolver = EventFlowOptions.New
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .CreateResolver();
            _msSqlConnection = _resolver.Resolve<IMsSqlConnection>();
            var migrator = _resolver.Resolve<IMsSqlDatabaseMigrator>();
            migrator.MigrateDatabaseUsingScripts(new []
                {
                    new SqlScript("test-table", @"
                        CREATE TABLE [Test] (
                            Id BIGINT NOT NULL PRIMARY KEY CLUSTERED IDENTITY(1, 1),
                            [Data] NVARCHAR(MAX) NOT NULL
                            )"),
                });
        }

        [TearDown]
        public void TearDown()
        {
            _resolver.Dispose();
            _testDatabase.Dispose();
        }

        public class Test
        {
            public long Id { get; set; }
            public string Data { get; set; }
        }

        [Test]
        public async Task BulkCopyAsync()
        {
            // Arrange
            const int count = 1000;
            var rows = (IReadOnlyCollection<Test>) Enumerable
                .Range(0, count)
                .Select(i => new Test {Id = i, Data = i.ToString()})
                .ToList();

            // Act
            var insertedRows = await _msSqlConnection.BulkCopyAsync(
                Label.Named("test"),
                "Test",
                rows,
                CancellationToken.None);

            // Assert
            insertedRows.Should().Be(count);
            var countFromDb = _testDatabase.Query<int>("SELECT COUNT(*) FROM [Test]").Single();
            countFromDb.Should().Be(count);
        }
    }
}
