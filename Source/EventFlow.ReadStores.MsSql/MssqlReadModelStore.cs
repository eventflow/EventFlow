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

using System;
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
    public class MssqlReadModelStore<TReadModel> :
        ReadModelStore<TReadModel>,
        IMssqlReadModelStore<TReadModel>
        where TReadModel : class, IMssqlReadModel, new()
    {
        private readonly IMsSqlConnection _connection;
        private readonly IQueryProcessor _queryProcessor;
        private readonly IReadModelSqlGenerator _readModelSqlGenerator;

        public MssqlReadModelStore(
            ILog log,
            IMsSqlConnection connection,
            IQueryProcessor queryProcessor,
            IReadModelSqlGenerator readModelSqlGenerator)
            : base(log)
        {
            _connection = connection;
            _queryProcessor = queryProcessor;
            _readModelSqlGenerator = readModelSqlGenerator;
        }

        public override async Task UpdateAsync(
            IReadOnlyCollection<ReadModelUpdate> readModelUpdates,
            IReadModelContext readModelContext,
            Func<IReadModelContext, IReadOnlyCollection<IDomainEvent>, ReadModelEnvelope<TReadModel>, CancellationToken, Task<ReadModelEnvelope<TReadModel>>> updateReadModel,
            CancellationToken cancellationToken)
        {
            // TODO: Transaction

            foreach (var readModelUpdate in readModelUpdates)
            {
                var readModelNameLowerCased = typeof(TReadModel).Name.ToLowerInvariant();
                var readModelEnvelope = await GetAsync(readModelUpdate.ReadModelId, cancellationToken).ConfigureAwait(false);
                var readModel = readModelEnvelope.ReadModel;
                var isNew = readModel == null;
                if (readModel == null)
                {
                    readModel = new TReadModel
                        {
                            AggregateId = readModelUpdate.ReadModelId,
                            CreateTime = readModelUpdate.DomainEvents.First().Timestamp,
                        };
                }

                readModelEnvelope = await updateReadModel(
                    readModelContext,
                    readModelUpdate.DomainEvents,
                    ReadModelEnvelope<TReadModel>.With(readModel, readModel.LastAggregateSequenceNumber),
                    cancellationToken)
                    .ConfigureAwait(false);

                readModel.UpdatedTime = DateTimeOffset.Now;
                readModel.LastAggregateSequenceNumber = (int) readModelEnvelope.Version.GetValueOrDefault();

                var sql = isNew
                    ? _readModelSqlGenerator.CreateInsertSql<TReadModel>()
                    : _readModelSqlGenerator.CreateUpdateSql<TReadModel>();

                await _connection.ExecuteAsync(
                    Label.Named("mssql-store-read-model", readModelNameLowerCased),
                    cancellationToken,
                    sql,
                    readModel).ConfigureAwait(false);
            }
        }

        public override async Task<ReadModelEnvelope<TReadModel>> GetAsync(string id, CancellationToken cancellationToken)
        {
            var readModelNameLowerCased = typeof(TReadModel).Name.ToLowerInvariant();
            var selectSql = _readModelSqlGenerator.CreateSelectSql<TReadModel>();
            var readModels = await _connection.QueryAsync<TReadModel>(
                Label.Named(string.Format("mssql-fetch-read-model-{0}", readModelNameLowerCased)),
                cancellationToken,
                selectSql,
                new { AggregateId = id })
                .ConfigureAwait(false);
            var readModel = readModels.SingleOrDefault();

            return readModel == null
                ? ReadModelEnvelope<TReadModel>.Empty
                : ReadModelEnvelope<TReadModel>.With(readModel, readModel.LastAggregateSequenceNumber);
        }

        public override Task DeleteAsync(string id, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public override async Task DeleteAllAsync(CancellationToken cancellationToken)
        {
            var sql = _readModelSqlGenerator.CreatePurgeSql<TReadModel>();
            var readModelName = typeof(TReadModel).Name;

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
    }
}
