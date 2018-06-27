using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Queries;

namespace EventFlow.EntityFramework.Tests.Model
{
    public class EfThingyGetVersionQueryHandler : IQueryHandler<ThingyGetVersionQuery, long?>
    {
        private readonly IDbContextProvider<TestDbContext> _dbContextProvider;

        public EfThingyGetVersionQueryHandler(IDbContextProvider<TestDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<long?> ExecuteQueryAsync(ThingyGetVersionQuery query, CancellationToken cancellationToken)
        {
            using (var context = _dbContextProvider.CreateContext())
            {
                var entity = await context.Thingys.FindAsync(query.ThingyId.Value);
                return entity.Version;
            }
        }
    }
}
