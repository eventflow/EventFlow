using System;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    public class SqliteDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly SqliteConnection _connection;

        public SqliteDbContextProvider()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
        }

        public TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;
            var context = new TestDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            _connection.Close();
        }
    }
}
