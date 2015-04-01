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

using System;
using System.Linq;
using System.Reflection;
using DbUp;
using EventFlow.Logs;
using EventFlow.MsSql.Exceptions;
using EventFlow.MsSql.Integrations;

namespace EventFlow.MsSql
{
    public class MsSqlDatabaseMigrator : IMsSqlDatabaseMigrator
    {
        private readonly ILog _log;
        private readonly IMsSqlConfiguration _msSqlConfiguration;

        public MsSqlDatabaseMigrator(
            ILog log,
            IMsSqlConfiguration msSqlConfiguration)
        {
            _log = log;
            _msSqlConfiguration = msSqlConfiguration;
        }

        public void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly)
        {
            MigrateDatabaseUsingEmbeddedScripts(assembly, _msSqlConfiguration.ConnectionString);
        }

        public void MigrateDatabaseUsingEmbeddedScripts(Assembly assembly, string connectionString)
        {
            var upgradeEngine = DeployChanges.To
                .SqlDatabase(connectionString)
                .WithScriptsEmbeddedInAssembly(assembly)
                .WithExecutionTimeout(TimeSpan.FromMinutes(5))
                .WithTransaction()
                .LogTo(new DbUpUpgradeLog(_log))
                .Build();

            var scripts = upgradeEngine.GetScriptsToExecute()
                .Select(s => s.Name)
                .ToList();

            _log.Information(
                "Going to migrate the MSSQL database by executing these scripts: {0}",
                string.Join(", ", scripts));

            var result = upgradeEngine.PerformUpgrade();
            if (!result.Successful)
            {
                throw new MssqlMigrationException(scripts, result.Error.Message, result.Error);
            }
        }
    }
}
