using System;
using System.Linq;
using EventFlow.Core;
using EventFlow.Extensions;
using EventFlow.MsSql.Tests.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    [Category(Categories.Integration)]
    public class IdentityIndexFragmentationTests : Test
    {
        private const int ROWS = 10000;
        private IMsSqlDatabase _testDatabase;

        private class MagicId : Identity<MagicId>
        {
            public MagicId(string value) : base(value)
            {
            }
        }

        [Test]
        public void VerifyThereLittleFragmentationUsingString()
        {
            // Act
            InsertRows(() => MagicId.NewComb().Value, ROWS, "IndexFragmentationString");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationString");
            fragmentation.Should().BeLessThan(10);
        }

        [Test]
        public void VerifyThereLittleFragmentationUsingGuid()
        {
            // Act
            InsertRows(() => MagicId.NewComb().GetGuid(), ROWS, "IndexFragmentationGuid");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationGuid");
            fragmentation.Should().BeLessThan(10);
        }

        [Test]
        public void SanityCombYieldsLowFragmentationStoredInGuid()
        {
            // Act
            InsertRows(GuidFactories.Comb.Create, ROWS, "IndexFragmentationGuid");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationGuid");
            fragmentation.Should().BeLessThan(10);
        }

        [Test]
        public void SanityCombYieldsLowFragmentationStoredInString()
        {
            // Act
            InsertRows(() => GuidFactories.Comb.Create().ToString("N"), ROWS, "IndexFragmentationString");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationString");
            fragmentation.Should().BeLessThan(10);
        }

        [Test]
        public void SanityGuidIdentityYieldsHighFragmentationStoredInString()
        {
            // Act
            InsertRows(() => MagicId.New.Value, ROWS, "IndexFragmentationString");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationString");
            fragmentation.Should().BeGreaterThan(30); // closer to 100 in reality
        }

        [Test]
        public void SanityGuidIdentityYieldsHighFragmentationStoredInGuid()
        {
            // Act
            InsertRows(() => MagicId.New.GetGuid(), ROWS, "IndexFragmentationGuid");

            // Assert
            var fragmentation = GetIndexFragmentation("IndexFragmentationGuid");
            fragmentation.Should().BeGreaterThan(30); // closer to 100 in reality
        }

        public void InsertRows<T>(Func<T> generator, int count, string table)
        {
            var ids = Enumerable.Range(0, count)
                .Select(_ => generator())
                .ToList();

            foreach (var id in ids)
            {
                _testDatabase.Execute($"INSERT INTO {table} (Id) VALUES (@Id)", new { Id = id });
            }
        }

        private double GetIndexFragmentation(string table)
        {
            const string sql = @"
                SELECT dbschemas.[name] as 'schema',
                dbtables.[name] as 'table',
                dbindexes.[name] as 'index',
                indexstats.avg_fragmentation_in_percent AS 'fragmentation',
                indexstats.page_count AS 'pageCount',
                index_level AS 'level'
                FROM sys.dm_db_index_physical_stats (DB_ID(), NULL, NULL, NULL, 'DETAILED') AS indexstats
                INNER JOIN sys.tables dbtables on dbtables.[object_id] = indexstats.[object_id]
                INNER JOIN sys.schemas dbschemas on dbtables.[schema_id] = dbschemas.[schema_id]
                INNER JOIN sys.indexes AS dbindexes ON dbindexes.[object_id] = indexstats.[object_id]
                AND indexstats.index_id = dbindexes.index_id
                WHERE indexstats.database_id = DB_ID()
                ORDER BY dbschemas.[name],dbtables.[name],dbindexes.[name],index_level  desc
                ";

            var rows = _testDatabase.Query<IndexFragmentationDetails>(sql)
                .Where(r => string.Equals(table, r.Table, StringComparison.OrdinalIgnoreCase))
                .OrderBy(r => r.Level)
                .ToList();
            
            return rows.First().Fragmentation;
        }

        [SetUp]
        public void SetUp()
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("index_fragmentation");
            _testDatabase.Execute("CREATE TABLE IndexFragmentationString (Id nvarchar(250) PRIMARY KEY)");
            _testDatabase.Execute("CREATE TABLE IndexFragmentationGuid (Id uniqueidentifier PRIMARY KEY)");
        }

        [TearDown]
        public void TearDown()
        {
            _testDatabase.DisposeSafe("DROP test database");
        }

        private class IndexFragmentationDetails
        {
            public IndexFragmentationDetails(
                string schema,
                string table,
                string index,
                double fragmentation, 
                long pageCount,
                byte level)
            {
                Schema = schema;
                Table = table;
                Index = index;
                Fragmentation = fragmentation;
                PageCount = pageCount;
                Level = level;
            }

            public string Schema { get; }
            public string Table { get; }
            public string Index { get; }

            public double Fragmentation { get; }
            public long PageCount { get; }

            public byte Level { get; }
        }
    }
}
