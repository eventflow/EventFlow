// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
// 

using DbUp;
using DbUp.Engine;
using EventFlow.Logs;
using EventFlow.Sql.Connections;
using EventFlow.Sql.Integrations;
using EventFlow.Sql.Migrations;
using EventFlow.SQLite.Connections;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFlow.SQLite.Migrations
{
    internal class SQLiteDatabaseMigrator : ISQLiteDatabaseMigrator
    {
        private readonly ILog _log;
        private readonly ISQLiteConfiguration _sqlConfiguration;

        public SQLiteDatabaseMigrator(
            ILog log,
            ISQLiteConfiguration sqlConfiguration)
        {
            _log = log;
            _sqlConfiguration = sqlConfiguration;
        }

        public void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly)
        {
            MigrateDatabaseUsingEmbeddedScripts(assembly, _sqlConfiguration.ConnectionString);
        }

        public void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly, string connectionString)
        {
            var upgradeEngine = DeployChanges.To
                .SQLiteDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(assembly)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(_log))
                .Build();

            Upgrade(upgradeEngine);
        }

        public void MigrateDatabaseUsingScripts(IEnumerable<Sql.Migrations.SqlScript> sqlScripts)
        {
            MigrateDatabaseUsingScripts(sqlScripts, _sqlConfiguration.ConnectionString);
        }

        public void MigrateDatabaseUsingScripts(IEnumerable<Sql.Migrations.SqlScript> sqlScripts, string connectionString)
        {
            var dbUpSqlScripts = sqlScripts.Select(s => new DbUp.Engine.SqlScript(s.Name, s.Content));
            var upgradeEngine = DeployChanges.To
                .SQLiteDatabase(connectionString)
                .WithScripts(dbUpSqlScripts)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(_log))
                .Build();

            Upgrade(upgradeEngine);
        }

        private void Upgrade(UpgradeEngine upgradeEngine)
        {
            var scripts = upgradeEngine.GetScriptsToExecute()
                .Select(s => s.Name)
                .ToList();

            _log.Information(
                "Going to migrate the SQLite database by executing these scripts: {0}",
                string.Join(", ", scripts));

            var result = upgradeEngine.PerformUpgrade();
            if (!result.Successful)
            {
                throw new Sql.Exceptions.SqlMigrationException(scripts, result.Error.Message, result.Error);
            }
        }
    }
}
