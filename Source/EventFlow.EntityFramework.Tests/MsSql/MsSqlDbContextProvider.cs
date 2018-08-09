using System;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.MsSql
{
    public class MsSqlDbContextProvider : IDbContextProvider<TestDbContext>, IDisposable
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public MsSqlDbContextProvider(IEntityFrameworkConfiguration configuration)
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseSqlServer(configuration.ConnectionString)
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
