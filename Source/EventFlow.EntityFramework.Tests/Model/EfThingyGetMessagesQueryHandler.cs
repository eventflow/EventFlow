using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.Queries;
using EventFlow.TestHelpers.Aggregates.Entities;
using EventFlow.TestHelpers.Aggregates.Queries;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests
{
    public class
        EfThingyGetMessagesQueryHandler : IQueryHandler<ThingyGetMessagesQuery, IReadOnlyCollection<ThingyMessage>>
    {
        private readonly IDbContextProvider<TestDbContext> _dbContextProvider;

        public EfThingyGetMessagesQueryHandler(IDbContextProvider<TestDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<IReadOnlyCollection<ThingyMessage>> ExecuteQueryAsync(ThingyGetMessagesQuery query,
            CancellationToken cancellationToken)
        {
            using (var context = _dbContextProvider.CreateContext())
            {
                var entities = await context.ThingyMessages
                    .Where(m => m.ThingyId == query.ThingyId.Value)
                    .Select(m => m.ToThingyMessage())
                    .ToArrayAsync(cancellationToken);
                return entities;
            }
        }
    }
}
