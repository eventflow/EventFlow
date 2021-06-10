// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using EventFlow.Sql.Connections;
using EventFlow.Sql.Exceptions;
using EventFlow.Sql.Integrations;
using Microsoft.Extensions.Logging;

namespace EventFlow.Sql.Migrations
{
    public abstract class SqlDatabaseMigrator<TConfiguration> : ISqlDatabaseMigrator
        where TConfiguration : ISqlConfiguration<TConfiguration>
    {
        protected ILogger Logger { get; }

        private readonly TConfiguration _sqlConfiguration;

        protected SqlDatabaseMigrator(
            ILogger logger,
            TConfiguration sqlConfiguration)
        {
            Logger = logger;
            _sqlConfiguration = sqlConfiguration;
        }

        public virtual void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly)
        {
            MigrateDatabaseUsingEmbeddedScripts(assembly, _sqlConfiguration.ConnectionString);
        }

        public virtual void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly, string connectionString)
        {
            var upgradeEngine = For(DeployChanges.To, connectionString)
                .WithScriptsEmbeddedInAssembly(assembly)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(Logger))
                .Build();

            Upgrade(upgradeEngine);
        }

        public void MigrateDatabaseUsingScripts(IEnumerable<SqlScript> sqlScripts)
        {
            MigrateDatabaseUsingScripts(sqlScripts, _sqlConfiguration.ConnectionString);
        }

        public virtual void MigrateDatabaseUsingScripts(IEnumerable<SqlScript> sqlScripts, string connectionString)
        {
            var dbUpSqlScripts = sqlScripts.Select(s => new DbUp.Engine.SqlScript(s.Name, s.Content));
            var upgradeEngine = For(DeployChanges.To, connectionString)
                .WithScripts(dbUpSqlScripts)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(Logger))
                .Build();

            Upgrade(upgradeEngine);
        }

        protected virtual void Upgrade(UpgradeEngine upgradeEngine)
        {
            var scripts = upgradeEngine.GetScriptsToExecute()
                .Select(s => s.Name)
                .ToList();

            Logger.LogInformation(
                "Going to migrate the SQL database by executing these scripts: {Scripts}",
                scripts);

            var result = upgradeEngine.PerformUpgrade();
            if (!result.Successful)
            {
                throw new SqlMigrationException(scripts, result.Error.Message, result.Error);
            }
        }

        protected abstract UpgradeEngineBuilder For(SupportedDatabases supportedDatabases, string connectionString);
    }
}
