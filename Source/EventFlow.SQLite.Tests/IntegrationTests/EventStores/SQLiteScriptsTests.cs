using System;
using System.Linq;
using EventFlow.SQLite.EventStores;
using EventFlow.TestHelpers;
using Helpz.SQLite;
using NUnit.Framework;
using EventFlow.Extensions;

namespace EventFlow.SQLite.Tests.IntegrationTests.EventStores
{
    [Category(Categories.Integration)]
    public class SQLiteScriptsTests
    {
        private ISQLiteDatabase _sqliteDatabase;

        [SetUp]
        public void SetUp()
        {
            _sqliteDatabase = SQLiteHelpz.CreateDatabase("eventflow");
        }

        [Test]
        public void SqlScriptsAreIdempotent()
        {
            // Arrange
            var sqlScripts = EventFlowEventStoresSQLite.GetSqlScripts().ToList();

            // Act
            foreach (var _ in Enumerable.Range(0, 2))
                foreach (var sqlScript in sqlScripts)
                    _sqliteDatabase.Execute(sqlScript.Content);
        }

        public void TearDown()
        {
            _sqliteDatabase.DisposeSafe("SQLite database");
        }
    }
}
