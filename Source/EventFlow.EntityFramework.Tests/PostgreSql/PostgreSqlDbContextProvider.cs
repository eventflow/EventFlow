using System;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.PostgreSql
{
    public class PostgreSqlDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public PostgreSqlDbContextProvider(IEntityFrameworkConfiguration configuration)
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseNpgsql(configuration.ConnectionString)
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
        }
    }
}
