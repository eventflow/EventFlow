// The MIT License (MIT)
//
// Copyright (c) 2015 Rasmus Mikkelsen
// https://github.com/rasmus/EventFlow
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

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventFlow.Aggregates;
using EventFlow.Core;
using EventFlow.Logs;
using EventFlow.MsSql;
using EventFlow.Queries;

namespace EventFlow.ReadStores.MsSql
{
    public class MssqlReadModelStore<TReadModel, TReadModelLocator> :
        ReadModelStore<TReadModel, TReadModelLocator>,
        IMssqlReadModelStore<TReadModel>
        where TReadModel : IMssqlReadModel, new()
        where TReadModelLocator : IReadModelLocator
    {
        private readonly IMsSqlConnection _connection;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IReadModelSqlGenerator _readModelSqlGenerator;

        public MssqlReadModelStore(
            ILog log,
            TReadModelLocator readModelLocator,
            IReadModelFactory readModelFactory,
            IMsSqlConnection connection,
            IQueryProcessor queryProcessor,
            IReadModelSqlGenerator readModelSqlGenerator)
            : base(log, readModelLocator, readModelFactory)
        {
            _connection = connection;
            _queryProcessor = queryProcessor;
            _readModelSqlGenerator = readModelSqlGenerator;
        }

        private async Task UpdateReadModelAsync(
            string id,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken)
        {
            var readModelNameLowerCased = typeof (TReadModel).Name.ToLowerInvariant();
            var readModel = await GetByIdAsync(id, cancellationToken).ConfigureAwait(false);
            var isNew = false;
            if (readModel == null)
            {
                isNew = true;
                readModel = new TReadModel
                    {
                        AggregateId = id,
                        CreateTime = domainEvents.First().Timestamp,
                    };
            }

            var appliedAny = await ReadModelFactory.UpdateReadModelAsync(
                readModel,
                domainEvents,
                readModelContext,
                cancellationToken)
                .ConfigureAwait(false);
            if (!appliedAny)
            {
                return;
            }

            var lastDomainEvent = domainEvents.Last();
            readModel.UpdatedTime = lastDomainEvent.Timestamp;
            readModel.LastAggregateSequenceNumber = lastDomainEvent.AggregateSequenceNumber;
            readModel.LastGlobalSequenceNumber = lastDomainEvent.GlobalSequenceNumber;

            var sql = isNew
                ? _readModelSqlGenerator.CreateInsertSql<TReadModel>()
                : _readModelSqlGenerator.CreateUpdateSql<TReadModel>();

            await _connection.ExecuteAsync(
                Label.Named("mssql-store-read-model", readModelNameLowerCased),
                cancellationToken,
                sql,
                readModel).ConfigureAwait(false);
        }

        public override Task<TReadModel> GetByIdAsync(
            string id,
            CancellationToken cancellationToken)
        {
            return _queryProcessor.ProcessAsync(new ReadModelByIdQuery<TReadModel>(id), cancellationToken);
        }

        public override async Task PurgeAsync<TReadModelToPurge>(CancellationToken cancellationToken)
        {
            if (typeof (TReadModel) != typeof(TReadModelToPurge))
            {
                return;
            }

            var sql = _readModelSqlGenerator.CreatePurgeSql<TReadModelToPurge>();
            var readModelName = typeof (TReadModelToPurge).Name;

            var rowsAffected = await _connection.ExecuteAsync(
                Label.Named("mssql-purge-read-model", readModelName),
                cancellationToken,
                sql)
                .ConfigureAwait(false);

            Log.Verbose(
                "Purge {0} read models of type '{1}'",
                rowsAffected,
                readModelName);
        }

        protected override Task UpdateReadModelsAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            CancellationToken cancellationToken)
        {
            var updateTasks = readModelUpdates
                .Select(rmu => UpdateReadModelAsync(rmu.ReadModelId, rmu.DomainEvents, readModelContext, cancellationToken));
            return Task.WhenAll(updateTasks);
        }
    }
}
