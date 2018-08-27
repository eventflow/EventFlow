using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EventFlow.EntityFramework.Extensions
{
    public static class Bulk
    {
        public static async Task<int> Delete<TContext, TEntity, TProjection>(IDbContextProvider<TContext> contextProvider,
            int batchSize,
            CancellationToken cancellationToken,
            Expression<Func<TEntity, TProjection>> projection,
            Expression<Func<TEntity, bool>> condition = null,
            Action<TProjection, EntityEntry<TEntity>> setProperties = null) 
            where TContext : DbContext 
            where TEntity : class, new()
        {
            int rowsAffected = 0;

            while (!cancellationToken.IsCancellationRequested)
                using (var dbContext = contextProvider.CreateContext())
                {
                    IQueryable<TEntity> query = dbContext
                        .Set<TEntity>()
                        .AsNoTracking();

                    if (condition != null)
                    {
                        query = query.Where(condition);
                    }

                    IEnumerable<TProjection> items = await query
                        .Take(batchSize)
                        .Select(projection)
                        .ToArrayAsync(cancellationToken)
                        .ConfigureAwait(false);

                    if (!items.Any())
                        return rowsAffected;

                    if (setProperties == null)
                    {
                        dbContext.RemoveRange((IEnumerable<object>) items);
                    }
                    else
                    {
                        foreach (var item in items)
                        {
                            var entity = new TEntity();
                            var entry = dbContext.Attach(entity);
                            setProperties.Invoke(item, entry);
                            entry.State = EntityState.Deleted;
                        }
                    }

                    rowsAffected += await dbContext.SaveChangesAsync(cancellationToken)
                        .ConfigureAwait(false);
                }

            return rowsAffected;
        }
    }
}
