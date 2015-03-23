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
using System.Reflection;
using System.Threading.Tasks;
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
        private readonly IMssqlConnection _connection;
        private static readonly string InsertSql = GetInsertSql();
        private static readonly string UpdateSql = GetUpdateSql();
        private static readonly string SelectSql = GetSelectSql();

        public MssqlReadModelStore(
            ILog log,
            IMssqlConnection connection)
            : base(log)
        {
            _connection = connection;
        }

        public override async Task UpdateReadModelAsync(string aggregateId, IReadOnlyCollection<IDomainEvent> domainEvents)
        {
            var readModels = await _connection.QueryAsync<TReadModel>(SelectSql, new {AggregateId = aggregateId}).ConfigureAwait(false);
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

            var sql = isNew ? InsertSql : UpdateSql;

            await _connection.ExecuteAsync(sql, readModel).ConfigureAwait(false);
        }

        public static string GetInsertSql()
        {
            return string.Format(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                GetTableName(),
                string.Join(", ", GetInsertColumns()),
                string.Join(", ", GetInsertColumns().Select(c => string.Format("@{0}", c))));
        }

        public static string GetUpdateSql()
        {
            return string.Format(
                "UPDATE {0} SET {1} WHERE AggregateId = @AggregateId",
                GetTableName(),
                string.Join(", ", GetUpdateColumns().Select(c => string.Format("{0} = @{0}", c))));
        }

        public static string GetSelectSql()
        {
            return string.Format("SELECT * FROM {0} WHERE AggregateId = @AggregateId", GetTableName());
        }

        public static IReadOnlyCollection<string> GetUpdateColumns()
        {
            return GetInsertColumns()
                .Where(c => c != "AggregateId")
                .ToList();
        }

        public static IReadOnlyCollection<string> GetInsertColumns()
        {
            return typeof (TReadModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name != "Id")
                .OrderBy(p => p.Name)
                .Select(p => p.Name)
                .ToList();
        }

        private static string GetTableName()
        {
            return string.Format("[ReadModel-{0}]", typeof (TReadModel).Name.Replace("ReadModel", string.Empty));
        }
    }
}
