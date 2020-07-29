// The MIT License (MIT)
// 
// Copyright (c) 2015-2020 Rasmus Mikkelsen
// Copyright (c) 2015-2020 eBay Software Foundation
// https://github.com/eventflow/EventFlow
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
// the Software, and to permit persons to whom the Software is furnished to do so,
// subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
// FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
// COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
// IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
// CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

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
