using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.AddEventFlowEvents();
        }
    }
}