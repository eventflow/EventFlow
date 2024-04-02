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

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using DbUp;
using DbUp.Builder;
using DbUp.Engine;
using EventFlow.Core;
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
        protected TConfiguration Configuration { get; }

        protected SqlDatabaseMigrator(
            ILogger logger,
            TConfiguration configuration)
        {
            Logger = logger;
            Configuration = configuration;
        }

        public virtual async Task MigrateDatabaseUsingEmbeddedScriptsAsync(
            Assembly assembly,
            string connectionStringName,
            CancellationToken cancellationToken)
        {
            var connectionString = await Configuration.GetConnectionStringAsync(
                Label.Named("migration"),
                connectionStringName,
                cancellationToken)
                .ConfigureAwait(false);

            await MigrateAsync(
                assembly,
                connectionString,
                cancellationToken)
                .ConfigureAwait(false);
        }

        protected virtual Task MigrateAsync(
            Assembly assembly,
            string connectionString,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    var upgradeEngine = For(DeployChanges.To, connectionString)
                        .WithScriptsEmbeddedInAssembly(assembly)
                        .WithExecutionTimeout(Configuration.UpgradeExecutionTimeout)
                        .WithTransaction()
                        .LogTo(new DbUpUpgradeLog(Logger))
                        .Build();

                    Upgrade(upgradeEngine);
                },
                TaskCreationOptions.LongRunning);
        }

        public async Task MigrateDatabaseUsingScriptsAsync(
            string connectionStringName,
            IEnumerable<SqlScript> sqlScripts,
            CancellationToken cancellationToken)
        {
            var connectionString = await Configuration.GetConnectionStringAsync(
                Label.Named("migration"),
                connectionStringName,
                cancellationToken)
                .ConfigureAwait(false);

            await MigrateAsync(sqlScripts, connectionString, cancellationToken).ConfigureAwait(false);
        }

        protected virtual Task MigrateAsync(
            IEnumerable<SqlScript> sqlScripts,
            string connectionString,
            CancellationToken cancellationToken)
        {
            return Task.Factory.StartNew(
                () =>
                {
                    var dbUpSqlScripts = sqlScripts.Select(s => new DbUp.Engine.SqlScript(s.Name, s.Content));
                    var upgradeEngine = For(DeployChanges.To, connectionString)
                        .WithScripts(dbUpSqlScripts)
                        .WithExecutionTimeout(Configuration.UpgradeExecutionTimeout)
                        .WithTransaction()
                        .LogTo(new DbUpUpgradeLog(Logger))
                        .Build();

                    Upgrade(upgradeEngine);
                },
                TaskCreationOptions.LongRunning);
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
