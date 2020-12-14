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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Core.RetryStrategies;
using EventFlow.Exceptions;
using EventFlow.Extensions;
using EventFlow.Logs;
using EventFlow.ReadStores;
using EventFlow.Sql.Connections;
using EventFlow.Sql.ReadModels.Attributes;

namespace EventFlow.Sql.ReadModels
{
    public abstract class SqlReadModelStore<TSqlConnection, TReadModel> :
        ReadModelStore<TReadModel>,
        ISqlReadModelStore<TReadModel>
        where TReadModel : class, IReadModel
        where TSqlConnection : ISqlConnection
    {
        private readonly TSqlConnection _connection;
        private readonly IReadModelSqlGenerator _readModelSqlGenerator;
        private readonly IReadModelFactory<TReadModel> _readModelFactory;
        private readonly ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> _transientFaultHandler;
        private static readonly Func<TReadModel, int?> GetVersion;
        private static readonly Action<TReadModel, int?> SetVersion;
        private static readonly Action<TReadModel, string> SetIdentity;

        static SqlReadModelStore()
        {
            var propertyInfos = typeof(TReadModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var versionPropertyInfo = propertyInfos
                .SingleOrDefault(p => p.GetCustomAttributes().Any(a => a is SqlReadModelVersionColumnAttribute));
            if (versionPropertyInfo == null)
            {
                GetVersion = rm => null as int?;
                SetVersion = (rm, v) => { };
            }
            else
            {
                GetVersion = rm => (int?)versionPropertyInfo.GetValue(rm);
                SetVersion = (rm, v) => versionPropertyInfo.SetValue(rm, v);
            }

            var identityPropertyInfo = propertyInfos
                .SingleOrDefault(p => p.GetCustomAttributes().Any(a => a is SqlReadModelIdentityColumnAttribute));
            if (identityPropertyInfo == null)
            {
                SetIdentity = (rm, i) => { };
            }
            else
            {
                SetIdentity = (rm, i) => identityPropertyInfo.SetValue(rm, i);
            }
        }

        protected SqlReadModelStore(
            ILog log,
            TSqlConnection connection,
            IReadModelSqlGenerator readModelSqlGenerator,
            IReadModelFactory<TReadModel> readModelFactory,
            ITransientFaultHandler<IOptimisticConcurrencyRetryStrategy> transientFaultHandler)
            : base(log)
        {
            _connection = connection;
            _readModelSqlGenerator = readModelSqlGenerator;
            _readModelFactory = readModelFactory;
            _transientFaultHandler = transientFaultHandler;
        }

        public override async Task UpdateAsync(IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken,
                Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            foreach (var readModelUpdate in readModelUpdates)
            {
                await _transientFaultHandler.TryAsync(
                    c => UpdateReadModelAsync(readModelContextFactory, updateReadModel, c, readModelUpdate),
                    Label.Named("sql-read-model-update"),
                    cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UpdateReadModelAsync(
            IReadModelContextFactory readModelContextFactory,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelUpdateResult<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken,
            ReadModelUpdate readModelUpdate)
        {
            var readModelId = readModelUpdate.ReadModelId;
            var readModelNameLowerCased = typeof(TReadModel).Name.ToLowerInvariant();
            var readModelEnvelope = await GetAsync(readModelId, cancellationToken).ConfigureAwait(false);
            var readModel = readModelEnvelope.ReadModel;
            var isNew = readModel == null;

            if (readModel == null)
            {
                readModel = await _readModelFactory.CreateAsync(readModelId, cancellationToken).ConfigureAwait(false);
                readModelEnvelope = ReadModelEnvelope<TReadModel>.With(readModelUpdate.ReadModelId, readModel);
            }

            var readModelContext = readModelContextFactory.Create(readModelId, isNew);

            var originalVersion = readModelEnvelope.Version;
            var readModelUpdateResult = await updateReadModel(
                readModelContext,
                readModelUpdate.DomainEvents,
                readModelEnvelope,
                cancellationToken)
                .ConfigureAwait(false);
            if (!readModelUpdateResult.IsModified)
            {
                return;
            }

            readModelEnvelope = readModelUpdateResult.Envelope;
            if (readModelContext.IsMarkedForDeletion)
            {
                await DeleteAsync(readModelId, cancellationToken).ConfigureAwait(false);
                return;
            }

            SetVersion(readModel, (int?) readModelEnvelope.Version);
            SetIdentity(readModel, readModelEnvelope.ReadModelId);

            var sql = isNew
                ? _readModelSqlGenerator.CreateInsertSql<TReadModel>()
                : _readModelSqlGenerator.CreateUpdateSql<TReadModel>();

            var dynamicParameters = new DynamicParameters(readModel);
            if (originalVersion.HasValue)
            {
                dynamicParameters.Add("_PREVIOUS_VERSION", (int)originalVersion.Value);
            }
            
            var rowsAffected = await _connection.ExecuteAsync(
                Label.Named("sql-store-read-model", readModelNameLowerCased),
                cancellationToken,
                sql,
                dynamicParameters)
                .ConfigureAwait(false);
            if (rowsAffected != 1)
            {
                throw new OptimisticConcurrencyException(
                    $"Read model '{readModelEnvelope.ReadModelId}' updated by another");
            }

            Log.Verbose(() => $"Updated SQL read model {typeof(TReadModel).PrettyPrint()} with ID '{readModelId}' to version '{readModelEnvelope.Version}'");
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
        {
            var readModelType = typeof(TReadModel);
            var readModelNameLowerCased = readModelType.Name.ToLowerInvariant();
            var selectSql = _readModelSqlGenerator.CreateSelectSql<TReadModel>();
            var readModels = await _connection.QueryAsync<TReadModel>(
                Label.Named(string.Format("sql-fetch-read-model-{0}", readModelNameLowerCased)),
                cancellationToken,
                selectSql,
                new { EventFlowReadModelId = id })
                .ConfigureAwait(false);

            var readModel = readModels.SingleOrDefault();

            if (readModel == null)
            {
                Log.Verbose(() => $"Could not find any SQL read model '{readModelType.PrettyPrint()}' with ID '{id}'");
                return ReadModelEnvelope<TReadModel>.Empty(id);
            }

            var readModelVersion = GetVersion(readModel);

            Log.Verbose(() => $"Found SQL read model '{readModelType.PrettyPrint()}' with ID '{readModelVersion}'");

            return ReadModelEnvelope<TReadModel>.With(id, readModel, readModelVersion);
        }

        public override async Task DeleteAsync(
            string id,
            CancellationToken cancellationToken)
        {
            var sql = _readModelSqlGenerator.CreateDeleteSql<TReadModel>();
            var readModelName = typeof(TReadModel).Name;

            var rowsAffected = await _connection.ExecuteAsync(
                Label.Named("sql-delete-read-model", readModelName),
                cancellationToken,
                sql,
                new { EventFlowReadModelId = id })
                .ConfigureAwait(false);

            if (rowsAffected != 0)
            {
                Log.Verbose($"Deleted read model '{id}' of type '{readModelName}'");
            }
        }

        public override async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            var sql = _readModelSqlGenerator.CreatePurgeSql<TReadModel>();
            var readModelName = typeof(TReadModel).Name;

            var rowsAffected = await _connection.ExecuteAsync(
                Label.Named("sql-purge-read-model", readModelName),
                cancellationToken,
                sql)
                .ConfigureAwait(false);

            Log.Verbose(
                "Purge {0} read models of type '{1}'",
                rowsAffected,
                readModelName);
        }
    }
}