using EventFlow.EntityFramework.Extensions;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.Model
{
    public class TestDbContext : DbContext
    {
        public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
        {
        }

        public DbSet<ThingyReadModelEntity> Thingys { get; set; }
        public DbSet<ThingyMessageReadModelEntity> ThingyMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder
                .AddEventFlowEvents()
                .AddEventFlowSnapshots();
        }
    }
}
