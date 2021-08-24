using System.Threading;
using System.Threading.Tasks;
using EventFlow.EntityFramework.Tests.Model;
using EventFlow.Queries;
using Microsoft.EntityFrameworkCore;

namespace EventFlow.EntityFramework.Tests.MsSql.IncludeTests.Queries
{
    public class PersonGetQuery : IQuery<Person>
    {
        public PersonId PersonId { get; }

        public PersonGetQuery(PersonId personId)
        {
            PersonId = personId;
        }
    }

    public class PersonGetQueryHandler : IQueryHandler<PersonGetQuery, Person>
    {
        private readonly IDbContextProvider<TestDbContext> _dbContextProvider;

        public PersonGetQueryHandler(IDbContextProvider<TestDbContext> dbContextProvider)
        {
            _dbContextProvider = dbContextProvider;
        }

        public async Task<Person> ExecuteQueryAsync(PersonGetQuery query, CancellationToken cancellationToken)
        {
            await using var context = _dbContextProvider.CreateContext();
            var entity = await context.Persons
                .Include(x => x.Addresses)
                .SingleOrDefaultAsync(x => x.AggregateId == query.PersonId.Value, cancellationToken)
                .ConfigureAwait(false);
            return entity?.ToPerson();
        }
    }
}