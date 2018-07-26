using System;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.SQLite
{
    public class SqliteDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly DbContextOptions<TestDbContext> _options;
        private readonly SqliteConnection _connection;

        public SqliteDbContextProvider()
        {
            // In-memory database only exists while the connection is open
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();

            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlite(_connection)
                .Options;
        }

        public TestDbContext CreateContext()
        {
            var context = new TestDbContext(_options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
            _connection.Dispose();
        }
    }
}
