using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public interface IUniqueConstraintDetectionStrategy
    {
        bool IsUniqueConstraintViolation(DbUpdateException exception);
    }
}
