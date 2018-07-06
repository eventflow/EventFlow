using EventFlow.EntityFramework.Tests.InMemory.Infrastructure;
using EventFlow.EntityFramework.Tests.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.InMemory.Storage.Internal;

namespace EventFlow.EntityFramework.Tests.InMemory
{
    public class InMemoryDbContextProvider : IDbContextProvider<TestDbContext>
    {
        private readonly DbContextOptions<TestDbContext> _options;

        public InMemoryDbContextProvider()
        {
            _options = new DbContextOptionsBuilder<TestDbContext>()
                .UseInMemoryDatabase("EventFlowTest")
                .ReplaceService<IInMemoryTableFactory, IndexingInMemoryTableFactory>()
                .Options;
        }

        public TestDbContext CreateContext()
        {
            var context = new TestDbContext(_options);
            context.Database.EnsureCreated();
            return context;
        }
    }
}
