using System.Threading;
using System.Threading.Tasks;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates;
using EventFlow.TestHelpers.Aggregates.Queries;

namespace EventFlow.EntityFramework.Tests.Model
{
    public class EfThingyGetQueryHandler : IQueryHandler<ThingyGetQuery, Thingy>
    {
        private readonly IDbContextProvider<TestDbContext> _dbContextProvider;

        public EfThingyGetQueryHandler(IDbContextProvider<TestDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<Thingy> ExecuteQueryAsync(ThingyGetQuery query, CancellationToken cancellationToken)
        {
            using (var context = _dbContextProvider.CreateContext())
            {
                var entity = await context.Thingys.FindAsync(query.ThingyId.Value);
                return entity?.ToThingy();
            }
        }
    }
}
