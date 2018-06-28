using System;

namespace EventFlow.TestHelpers.Aggregates.Queries
{
    public class DbContext : IDbContext
    {
        public string Id { get; } = Guid.NewGuid().ToString();
    }
}