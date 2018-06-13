using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public interface IDbContextProvider
    {
        DbContext CreateContext();
    }
}