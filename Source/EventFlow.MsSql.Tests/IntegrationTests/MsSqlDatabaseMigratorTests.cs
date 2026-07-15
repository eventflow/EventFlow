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

using System;
using EventFlow.Configuration;
using EventFlow.MsSql.EventStores;
using EventFlow.MsSql.Extensions;
using EventFlow.TestHelpers;
using EventFlow.TestHelpers.MsSql;
using NUnit.Framework;

namespace EventFlow.MsSql.Tests.IntegrationTests
{
    /// <summary>
    /// Exercises the DbUp integration end-to-end (UpgradeEngine + DbUpUpgradeLog) against
    /// a real SQL Server. On netcoreapp3.1 this runs against dbup-core 5.x, on net10.0
    /// against dbup-core 6.x, proving both TFM-specific dependency sets migrate correctly.
    /// </summary>
    [Category(Categories.Integration)]
    public class MsSqlDatabaseMigratorTests
    {
        private IMsSqlDatabase _testDatabase;
        private IRootResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            _testDatabase = MsSqlHelpz.CreateDatabase("eventflow-migrator");
            _resolver = EventFlowOptions.New
                .ConfigureMsSql(MsSqlConfiguration.New.SetConnectionString(_testDatabase.ConnectionString.Value))
                .CreateResolver();
        }

        [TearDown]
        public void TearDown()
        {
            _resolver?.Dispose();
            _testDatabase?.Dispose();
        }

        [Test]
        public void EmbeddedScriptsAreMigratedAndIdempotent()
        {
            // Arrange
            var databaseMigrator = _resolver.Resolve<IMsSqlDatabaseMigrator>();

            // Act (twice - DbUp journals executed scripts, so the second run must be a no-op)
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);
            EventFlowEventStoresMsSql.MigrateDatabase(databaseMigrator);

            // Assert - the migrated table exists and is queryable
            _testDatabase.Execute("SELECT TOP 1 GlobalSequenceNumber FROM [dbo].[EventFlow]");
        }
    }
}
