// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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

using EventFlow.PostgreSql.ReadModels;
using EventFlow.Sql.ReadModels;
using EventFlow.ReadStores;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.PostgreSql.Tests.UnitTests
{
    [TestFixture]
    public class ReadModelSqlGeneratorTests
    {
        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInSelectQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var selectSql = readModelSqlGenerator.CreateSelectSql<TestReadModel>();

            // Assert
            selectSql.Should().Contain("\"AggregateId\" = @EventFlowReadModelId");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInInsertQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var insertSql = readModelSqlGenerator.CreateInsertSql<TestReadModel>();

            // Assert
            insertSql.Should().Contain("\"AggregateId\", \"Name\"");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInUpdateQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var updateSql = readModelSqlGenerator.CreateUpdateSql<TestReadModel>();

            // Assert
            updateSql.Should().Contain("\"AggregateId\" = @Id");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInDeleteQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var deleteSql = readModelSqlGenerator.CreateDeleteSql<TestReadModel>();

            // Assert
            deleteSql.Should().Contain("\"AggregateId\" = @EventFlowReadModelId");
        }

        public class TestReadModel : IReadModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
