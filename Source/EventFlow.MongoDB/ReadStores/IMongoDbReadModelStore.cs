using EventFlow.ReadStores;
using System;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using System.Linq;

namespace EventFlow.MongoDB.ReadStores
{
	public interface IMongoDbReadModelStore<TReadModel> : IReadModelStore<TReadModel>
		where TReadModel : class, IReadModel, new()
	{
		Task<IAsyncCursor<TReadModel>> FindAsync(
			Expression<Func<TReadModel, bool>> filter,
			FindOptions<TReadModel, TReadModel> options = null,
			CancellationToken cancellationToken = default);
	}
}