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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Logs;
using EventFlow.MsSql.Tests.Helpers;
using EventFlow.ReadStores.MsSql;
using EventFlow.ReadStores.MsSql.TableGeneration;
using EventFlow.Test;
using FluentAssertions;
using NUnit.Framework;
using Ploeh.AutoFixture;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    public class TableTypeReaderTest : TestsFor<TableTypeReader>
    {
        private ITestDatabase _testDatabase;
        private IMsSqlConnection _msSqlConnection;

        [SetUp]
        public void SetUp()
        {
            _testDatabase = MsSqlHelper.CreateDatabase("TableTypeReader");

            var msSqlConfigurationMock = Freze<IMsSqlConfiguration>();
            msSqlConfigurationMock
                .Setup(c => c.ConnectionString)
                .Returns(_testDatabase.ConnectionString);

            Fixture.Inject<ILog>(new ConsoleLog());
            _msSqlConnection = Fixture.Create<MsSqlConnection>();
            Fixture.Inject(_msSqlConnection);
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.Dispose();
        }

        [Test]
        public async Task NoneExistingTable_ReturnsEmpty()
        {
            // Act
            var columnDescriptions = await Sut.GetColumnDescriptionsAsync(A<string>(), CancellationToken.None).ConfigureAwait(false);

            // Assert
            columnDescriptions.Should().BeEmpty();
        }

        [Test]
        public async Task Hh()
        {
            // Arrange
            var tableName = A<string>();
            var columns = new[]
                {
                    "[AggregateId] [nvarchar](64) NOT NULL",
                    "[Comment] [nvarchar](MAX) NOT NULL",
                    "[CreateTime] [datetimeoffset](7) NOT NULL",
                    "[DomainErrorAfterFirstReceived] [bit] NOT NULL",
                    "[LastAggregateSequenceNumber] [int] NOT NULL",
                    "[LastGlobalSequenceNumber] [bigint] NOT NULL",
                    "[PingsReceived] [int] NOT NULL",
                    "[UpdatedTime] [datetimeoffset](7) NOT NULL",
                };
            await CreateTableAsync(tableName, columns).ConfigureAwait(false);

            // Act
            var columnDescriptions = await Sut.GetColumnDescriptionsAsync(tableName, CancellationToken.None).ConfigureAwait(false);

            // Assert
            var textDescriptions = columnDescriptions.Select(c => c.ToString()).ToArray();
            textDescriptions.ShouldAllBeEquivalentTo(columns);
        }

        private async Task CreateTableAsync(string tableName, IEnumerable<string> columns)
        {
            var sql = string.Format(
                "CREATE TABLE [dbo].[{0}]({1})",
                tableName,
                string.Join(", ", columns));
            await _msSqlConnection.ExecuteAsync(CancellationToken.None, sql);
        }
    }
}
