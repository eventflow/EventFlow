using System;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.PostgreSql
{
    public class PostgreSqlDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly string _connectionString;

        public PostgreSqlDbContextProvider(IEntityFrameworkConfiguration entityFrameworkConfiguration)
        {
            _connectionString = entityFrameworkConfiguration.ConnectionString;
        }

        public TestDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<TestDbContext>()
                .UseNpgsql(_connectionString)
                .Options;
            var context = new TestDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        public void Dispose()
        {
        }
    }
}
