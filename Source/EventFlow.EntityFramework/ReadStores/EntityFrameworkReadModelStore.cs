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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.EntityFramework.Extensions;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;

namespace EventFlow.EntityFramework.ReadStores
{
    public class EntityFrameworkReadModelStore<TReadModel, TDbContext> :
        ReadModelStore<TReadModel>,
        IEntityFrameworkReadModelStore<TReadModel>
        where TReadModel : class, IReadModel, new()
        where TDbContext : DbContext
    {
        private static readonly ConcurrentDictionary<string, EntityDescriptor> Descriptors
            = new ConcurrentDictionary<string, EntityDescriptor>();

        private static readonly string ReadModelNameLowerCase = typeof(TReadModel).Name.ToLowerInvariant();

        private readonly IDbContextProvider<TDbContext> _contextProvider;
        private readonly int _deletionBatchSize;
        private readonly IReadModelFactory<TReadModel> _readModelFactory;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;

        public EntityFrameworkReadModelStore(
            IBulkOperationConfiguration bulkOperationConfiguration,
            ILog log,
            IReadModelFactory<TReadModel> readModelFactory,
            IDbContextProvider<TDbContext> contextProvider,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
            : base(log)
        {
            _readModelFactory = readModelFactory;
            _contextProvider = contextProvider;
            _transientFaultHandler = transientFaultHandler;
            _deletionBatchSize = bulkOperationConfiguration.DeletionBatchSize;
        }

        public override async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            using (var dbContext = _contextProvider.CreateContext())
            {
                foreach (var readModelUpdate in readModelUpdates)
                {
                    await _transientFaultHandler.TryAsync(
                            c => UpdateReadModelAsync(
                                // ReSharper disable once AccessToDisposedClosure
                                dbContext,
                                readModelContextFactory,
                                updateReadModel,
                                c,
                                readModelUpdate),
                            Label.Named("efcore-read-model-update"),
                            cancellationToken)
                        .ConfigureAwait(false);

                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id,
            CancellationToken cancellationToken)
        {
            using (var dbContext = _contextProvider.CreateContext())
            {
                return await GetAsync(dbContext, id, cancellationToken).ConfigureAwait(false);
            }
        }

        public override async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            using (var dbContext = _contextProvider.CreateContext())
            {
                await DeleteAsync(dbContext, id, cancellationToken).ConfigureAwait(false);
            }
        }

        private class BulkDeletionModel
        {
            public string Id { get; set; }
            public long? Version { get; set; }
        }

        public override async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            var readModelName = typeof(TReadModel).Name;

            EntityDescriptor descriptor;
            using (var dbContext = _contextProvider.CreateContext())
            {
                descriptor = GetDescriptor(dbContext);
            }

            var rowsAffected = await Bulk.Delete<TDbContext, TReadModel, BulkDeletionModel>(
                _contextProvider,
                _deletionBatchSize,
                cancellationToken,
                entity => new BulkDeletionModel
                {
                    Id = EF.Property<string>(entity, descriptor.Key),
                    Version = EF.Property<long>(entity, descriptor.Version)
                },
                setProperties: (model, entry) =>
                {
                    descriptor.SetId(entry, model.Id);
                    descriptor.SetVersion(entry, model.Version);
                })
                .ConfigureAwait(false);

            Log.Verbose(
                "Purge {0} read models of type '{1}'",
                rowsAffected,
                readModelName);
        }

        private async Task<ReadModelEnvelope<TReadModel>> GetAsync(TDbContext dbContext,
            string id,
            CancellationToken cancellationToken,
            bool tracking = false)
        {
            var readModelType = typeof(TReadModel);
            var descriptor = GetDescriptor(dbContext);
            var entity = await descriptor.Query(dbContext, id, cancellationToken, tracking)
                .ConfigureAwait(false);

            if (entity == null)
            {
                Log.Verbose(() => $"Could not find any Entity Framework read model '{readModelType.PrettyPrint()}' with ID '{id}'");
                return ReadModelEnvelope<TReadModel>.Empty(id);
            }

            var entry = dbContext.Entry(entity);
            var version = descriptor.GetVersion(entry);

            Log.Verbose(() => $"Found Entity Framework read model '{readModelType.PrettyPrint()}' with ID '{id}' and version '{version}'");

            return version.HasValue
                ? ReadModelEnvelope<TReadModel>.With(id, entity, version.Value)
                : ReadModelEnvelope<TReadModel>.With(id, entity);
        }

        private async Task DeleteAsync(TDbContext dbContext, string id, CancellationToken cancellationToken)
        {
            var entity = await dbContext.Set<TReadModel>().FindAsync(id).ConfigureAwait(false);
            if (entity == null)
                return;
            dbContext.Remove(entity);
            var rowsAffected = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

            if (rowsAffected != 0)
            {
                Log.Verbose($"Deleted Entity Framework read model '{id}' of type '{ReadModelNameLowerCase}'");
            }
        }

        private async Task UpdateReadModelAsync(TDbContext dbContext, IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken,
            ReadModelUpdate readModelUpdate)
        {
            var readModelId = readModelUpdate.ReadModelId;
            var readModelEnvelope = await GetAsync(dbContext, readModelId, cancellationToken, true)
                .ConfigureAwait(false);

            var entity = readModelEnvelope.ReadModel;
            var isNew = entity == null;

            if (entity == null)
            {
                entity = await _readModelFactory.CreateAsync(readModelId, cancellationToken).ConfigureAwait(false);
                readModelEnvelope = ReadModelEnvelope<TReadModel>.With(readModelId, entity);
            }

            var readModelContext = readModelContextFactory.Create(readModelId, isNew);
            var originalVersion = readModelEnvelope.Version;
            var updateResult = await updateReadModel(
                    readModelContext,
                    readModelUpdate.DomainEvents,
                    readModelEnvelope,
                    cancellationToken)
                .ConfigureAwait(false);

            if (!updateResult.IsModified)
                return;

            if (readModelContext.IsMarkedForDeletion)
            {
                await DeleteAsync(dbContext, readModelId, cancellationToken).ConfigureAwait(false);
                return;
            }

            readModelEnvelope = updateResult.Envelope;
            entity = readModelEnvelope.ReadModel;

            var descriptor = GetDescriptor(dbContext);
            var entry = isNew
                ? dbContext.Add(entity)
                : dbContext.Entry(entity);
            descriptor.SetId(entry, readModelId);
            descriptor.SetVersion(entry, originalVersion, readModelEnvelope.Version);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (DbUpdateConcurrencyException e)
            {
                var databaseValues = await entry.GetDatabaseValuesAsync(cancellationToken)
                    .ConfigureAwait(false);
                entry.CurrentValues.SetValues(databaseValues);
                throw new OptimisticConcurrencyException(e.Message, e);
            }

            Log.Verbose(() => $"Updated Entity Framework read model {typeof(TReadModel).PrettyPrint()} with ID '{readModelId}' to version '{readModelEnvelope.Version}'");
        }

        private static EntityDescriptor GetDescriptor(DbContext context)
        {
            return Descriptors.GetOrAdd(context.Database.ProviderName, s =>
                new EntityDescriptor(context));
        }

        private class EntityDescriptor
        {
            private readonly IProperty _key;
            private readonly Func<DbContext, CancellationToken, string, Task<TReadModel>> _queryByIdNoTracking;
            private readonly Func<DbContext, CancellationToken, string, Task<TReadModel>> _queryByIdTracking;
            private readonly IProperty _version;

            public EntityDescriptor(DbContext context)
            {
                var entityType = context.Model.FindEntityType(typeof(TReadModel));
                _key = GetKeyProperty(entityType);
                _version = GetVersionProperty(entityType);
                _queryByIdTracking = CompileQueryById(true);
                _queryByIdNoTracking = CompileQueryById(false);
            }

            public string Key => _key.Name;
            public string Version => _version.Name;

            public Task<TReadModel> Query(DbContext context, string id, CancellationToken t, bool tracking = false)
            {
                return tracking
                    ? _queryByIdTracking(context, t, id)
                    : _queryByIdNoTracking(context, t, id);
            }

            public void SetId(EntityEntry entry, string id)
            {
                var property = entry.Property(_key.Name);
                property.CurrentValue = id;
            }

            public long? GetVersion(EntityEntry entry)
            {
                if (_version == null) return null;

                var property = entry.Property(_version.Name);
                return (long?) property.CurrentValue;
            }

            public void SetVersion(EntityEntry entry, long? originalVersion, long? currentVersion = null)
            {
                if (_version == null) return;

                var property = entry.Property(_version.Name);
                property.OriginalValue = originalVersion ?? 0;
                property.CurrentValue = currentVersion ?? 0;
            }

            private bool IsConcurrencyProperty(IProperty p)
            {
                return p.IsConcurrencyToken && (p.ClrType == typeof(long) || p.ClrType == typeof(byte[]));
            }

            private static IProperty GetKeyProperty(IEntityType entityType)
            {
                IProperty key;
                var keyProperties = entityType.FindPrimaryKey() ??
                                    throw new InvalidOperationException("Primary key not found");
                try
                {
                    key = keyProperties.Properties.Single();
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException("Read store doesn't support composite primary keys.", e);
                }

                return key;
            }

            private IProperty GetVersionProperty(IEntityType entityType)
            {
                IProperty version;
                var concurrencyProperties = entityType
                    .GetProperties()
                    .Where(IsConcurrencyProperty)
                    .ToList();

                if (concurrencyProperties.Count > 1)
                    concurrencyProperties = concurrencyProperties
                        .Where(p => p.Name.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0)
                        .ToList();

                try
                {
                    version = concurrencyProperties.SingleOrDefault();
                }
                catch (InvalidOperationException e)
                {
                    throw new InvalidOperationException("Couldn't determine row version property.", e);
                }

                return version;
            }

            private Func<DbContext, CancellationToken, string, Task<TReadModel>> CompileQueryById(bool tracking)
            {
                return tracking
                    ? EF.CompileAsyncQuery((DbContext dbContext, CancellationToken t, string id) =>
                        dbContext
                            .Set<TReadModel>()
                            .AsTracking()
                            .SingleOrDefault(e => EF.Property<string>(e, Key) == id))
                    : EF.CompileAsyncQuery((DbContext dbContext, CancellationToken t, string id) =>
                        dbContext
                            .Set<TReadModel>()
                            .AsNoTracking()
                            .SingleOrDefault(e => EF.Property<string>(e, Key) == id));
            }
        }
    }
}