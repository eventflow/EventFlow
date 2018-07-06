using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Extensions
{
    public static class ExceptionExtensions
    {
        public static bool IsUniqueConstraintViolation(this DbUpdateException e,
            IUniqueConstraintDetectionStrategy strategy)
        {
            return strategy.IsUniqueConstraintViolation(e);
        }
    }
}
