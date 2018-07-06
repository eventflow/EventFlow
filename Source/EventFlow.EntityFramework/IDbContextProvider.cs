using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public interface IDbContextProvider<out TDbContext> where TDbContext : DbContext
    {
        TDbContext CreateContext();
    }
}
