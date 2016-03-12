using System;
using System.Linq;
using System.Reflection;
using DbUp;
using EventFlow.Logs;
using EventFlow.Sql.Exceptions;
using EventFlow.Sql.Integrations;
using EventFlow.Sql.Migrations;

namespace EventFlow.SQLite.Connections
{
    public class SQLiteDatabaseMigrator : SqlDatabaseMigrator<ISQLiteConfiguration>, ISQLiteDatabaseMigrator
    {
        public SQLiteDatabaseMigrator(
            ILog log,
            ISQLiteConfiguration sqlConfiguration)
            : base(log, sqlConfiguration)
        {
            _log = log;
            _sqlConfiguration = sqlConfiguration;
        }

        public new void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly)
        {
            MigrateDatabaseUsingEmbeddedScripts(assembly, _sqlConfiguration.ConnectionString);
        }

        public new void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly, string connectionString)
        {
            // TODO { new ExampleAction("SQLite", Deploy(to => to.SQLiteDatabase(string.Empty), (builder, schema, tableName) => { builder.Configure(c => c.Journal = new SQLiteTableJournal(()=>c.ConnectionManager, ()=>c.Log, tableName)); return builder; })) },
            var upgradeEngine = DeployChanges.To
                .SQLiteDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(assembly)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(_log))
                .Build();

            var scripts = upgradeEngine.GetScriptsToExecute()
                .Select(s => s.Name)
                .ToList();

            _log.Information(
                "Going to migrate the SQL database by executing these scripts: {0}",
                string.Join(", ", scripts));

            var result = upgradeEngine.PerformUpgrade();
            if (!result.Successful)
            {
                throw new SqlMigrationException(scripts, result.Error.Message, result.Error);
            }
        }

        private readonly ILog _log;

        private readonly ISQLiteConfiguration _sqlConfiguration;

    }
}