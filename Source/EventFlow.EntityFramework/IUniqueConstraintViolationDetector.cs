using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    public interface IUniqueConstraintViolationDetector
    {
        bool IsUniqueContraintException(DbUpdateException exception);
    }
}
