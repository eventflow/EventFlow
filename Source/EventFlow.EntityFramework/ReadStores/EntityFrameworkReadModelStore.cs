// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace EventFlow.EntityFramework.ReadStores
{
    public class EntityFrameworkReadModelStore<TReadModel, TDbContext> :
        ReadModelStore<TReadModel>,
        IEntityFrameworkReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
        where TDbContext : DbContext
    {
        private readonly IDbContextProvider<TDbContext> _contextProvider;
        private readonly IReadModelFactory<TReadModel> _readModelFactory;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public EntityFrameworkReadModelStore(
            ILog log,
            IReadModelFactory<TReadModel> readModelFactory,
            IDbContextProvider<TDbContext> contextProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
            : base(log)
        {
            _readModelFactory = readModelFactory;
            _contextProvider = contextProvider;
            _transientFaultHandler = transientFaultHandler;
        }

        public override async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            Func<IReadModelContext> readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            foreach (var readModelUpdate in readModelUpdates)
            {
                var readModelContext = readModelContextFactory();

                await _transientFaultHandler.TryAsync(
                        c => UpdateReadModelAsync(readModelContext, updateReadModel, c, readModelUpdate),
                        Label.Named("efcore-read-model-update"),
                        cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id,
            CancellationToken cancellationToken)
        {
            using (var dbContext = _contextProvider.CreateContext())
            {
                var entity = await dbContext.FindAsync<TReadModel>(id);
                if (entity == null)
                    return ReadModelEnvelope<TReadModel>.Empty(id);

                var entry = dbContext.Entry(entity);
                var version = entry.Metadata.GetProperties().SingleOrDefault(p => p.IsConcurrencyToken);
                if (version != null)
                {
                    var versionValue = (long) entry.Property(version.Name).OriginalValue;
                    return ReadModelEnvelope<TReadModel>.With(id, entity, versionValue);
                }

                return ReadModelEnvelope<TReadModel>.With(id, entity);
            }
        }

        public override async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            using (var dbContext = _contextProvider.CreateContext())
            {
                // TODO: Delete without loading whole entity first (just for the Version)

                var entity = await dbContext.Set<TReadModel>().FindAsync(id);
                if (entity == null)
                    return;
                dbContext.Remove(entity);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        public override Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task UpdateReadModelAsync(
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken,
            ReadModelUpdate readModelUpdate)
        {
            var readModelId = readModelUpdate.ReadModelId;
            var readModelEnvelope = await GetAsync(readModelId, cancellationToken).ConfigureAwait(false);
            var readModel = readModelEnvelope.ReadModel;
            var isNew = readModel == null;

            if (readModel == null)
            {
                readModel = await _readModelFactory.CreateAsync(readModelId, cancellationToken).ConfigureAwait(false);
                readModelEnvelope = ReadModelEnvelope<TReadModel>.With(readModelId, readModel);
            }

            var originalVersion = readModelEnvelope.Version;
            readModelEnvelope = await updateReadModel(
                    readModelContext,
                    readModelUpdate.DomainEvents,
                    readModelEnvelope,
                    cancellationToken)
                .ConfigureAwait(false);

            if (readModelContext.IsMarkedForDeletion)
            {
                await DeleteAsync(readModelId, cancellationToken);
                return;
            }

            using (var dbContext = _contextProvider.CreateContext())
            {
                var entry = dbContext.Attach(readModelEnvelope.ReadModel);
                SetId(entry, readModelId);
                var version = entry.Metadata.GetProperties().SingleOrDefault(p => p.IsConcurrencyToken);
                if (version != null)
                {
                    entry.Property(version.Name).OriginalValue = originalVersion ?? 0;
                    entry.Property(version.Name).CurrentValue = readModelEnvelope.Version;
                }
                entry.State = isNew ? EntityState.Added : EntityState.Modified;
                try
                {
                    await dbContext.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateConcurrencyException e)
                {
                    throw new OptimisticConcurrencyException(e.Message, e);
                }
            }
        }

        private static void SetId(EntityEntry<TReadModel> entry, string readModelId)
        {
            var key = entry.Metadata.FindPrimaryKey().Properties.Single().Name;
            entry.Property(key).CurrentValue = readModelId;
        }
    }
}
