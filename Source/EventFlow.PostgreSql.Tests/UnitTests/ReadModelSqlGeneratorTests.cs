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
            selectSql.Should().Contain("\"Id\" = @EventFlowReadModelId");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInInsertQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var insertSql = readModelSqlGenerator.CreateInsertSql<TestReadModel>();

            // Assert
            insertSql.Should().Contain("\"Id\", \"Name\"");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInUpdateQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var updateSql = readModelSqlGenerator.CreateUpdateSql<TestReadModel>();

            // Assert
            updateSql.Should().Contain("\"Id\" = @Id");
        }

        [Test]
        public void PostgresReadModelSqlGenerator_ApplyColumnPrefixesAndSuffixesInDeleteQueries()
        {
            // Arrange
            var readModelSqlGenerator = new PostgresReadModelSqlGenerator();

            // Act
            var deleteSql = readModelSqlGenerator.CreateDeleteSql<TestReadModel>();

            // Assert
            deleteSql.Should().Contain("\"Id\" = @EventFlowReadModelId");
        }

        public class TestReadModel : IReadModel
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }
    }
}
