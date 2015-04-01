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
using EventFlow.Logs;
using EventFlow.MsSql;

namespace EventFlow.ReadStores.MsSql
{
    public class MssqlReadModelStore<TAggregate, TReadModel> :
        ReadModelStore<TAggregate, TReadModel>,
        IMssqlReadModelStore<TAggregate, TReadModel>
        where TReadModel : IMssqlReadModel, new()
        where TAggregate : IAggregateRoot
    {
        private readonly IMsSqlConnection _connection;
        private readonly IReadModelSqlGenerator _readModelSqlGenerator;

        public MssqlReadModelStore(
            ILog log,
            IMsSqlConnection connection,
            IReadModelSqlGenerator readModelSqlGenerator)
            : base(log)
        {
            _connection = connection;
            _readModelSqlGenerator = readModelSqlGenerator;
        }

        public override async Task UpdateReadModelAsync(
            string aggregateId,
            IReadOnlyCollection<IDomainEvent> domainEvents,
            CancellationToken cancellationToken)
        {
            var selectSql = _readModelSqlGenerator.CreateSelectSql<TReadModel>();
            var readModels = await _connection.QueryAsync<TReadModel>(
                cancellationToken,
                selectSql,
                new { AggregateId = aggregateId })
                .ConfigureAwait(false);
            var readModel = readModels.SingleOrDefault();
            var isNew = false;
            if (readModel == null)
            {
                isNew = true;
                readModel = new TReadModel
                    {
                        AggregateId = aggregateId,
                        CreateTime = domainEvents.First().Timestamp,
                    };
            }

            ApplyEvents(readModel, domainEvents);

            var lastDomainEvent = domainEvents.Last();
            readModel.UpdatedTime = lastDomainEvent.Timestamp;
            readModel.LastAggregateSequenceNumber = lastDomainEvent.AggregateSequenceNumber;
            readModel.LastGlobalSequenceNumber = lastDomainEvent.GlobalSequenceNumber;

            var sql = isNew
                ? _readModelSqlGenerator.CreateInsertSql<TReadModel>()
                : _readModelSqlGenerator.CreateUpdateSql<TReadModel>();

            await _connection.ExecuteAsync(cancellationToken, sql, readModel).ConfigureAwait(false);
        }
    }
}
