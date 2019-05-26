using System.Collections.Generic;
using System.Linq;
using Dapper;
using EventFlow.TestHelpers.MsSql;

namespace EventFlow.MsSql.Tests.Extensions
{
    public static class MsSqlDatabaseExtensions
    {
        public static IReadOnlyCollection<T> Query<T>(this IMsSqlDatabase database, string sql)
        {
            return database.WithConnection<IReadOnlyCollection<T>>(c => c.Query<T>(sql).ToList());
        }

        public static int Execute(this IMsSqlDatabase database, string sql, object param)
        {
            return database.WithConnection(c => c.Execute(sql, param));
        }
    }
}
