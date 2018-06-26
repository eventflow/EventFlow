using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;

namespace EventFlow.TestHelpers.Aggregates.Queries
{
    public class DbContextQueryHandler : IQueryHandler<DbContextQuery, string>
    {
        private readonly IDbContext _dbContext;

        public DbContextQueryHandler(IDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Task<string> ExecuteQueryAsync(DbContextQuery query, CancellationToken cancellationToken)
        {
            return Task.FromResult(_dbContext.Id);
        }
    }
}