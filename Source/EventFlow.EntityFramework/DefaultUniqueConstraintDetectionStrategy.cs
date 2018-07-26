using System;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework
{
    internal class DefaultUniqueConstraintDetectionStrategy : IUniqueConstraintDetectionStrategy
    {
        public bool IsUniqueConstraintViolation(DbUpdateException exception)
        {
            return exception.InnerException?.Message?.IndexOf("unique", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}