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
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using EventFlow.Extensions;
using EventFlow.ReadStores;
using EventFlow.Sql.Extensions;
using EventFlow.Sql.ReadModels.Attributes;

namespace EventFlow.Sql.ReadModels
{
    public class ReadModelSqlGenerator : IReadModelSqlGenerator
    {
        protected ReadModelSqlGeneratorConfiguration Configuration;

        private static readonly ConcurrentDictionary<Type, string>
            TableNames = new ConcurrentDictionary<Type, string>();

        private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>> PropertyInfos =
            new ConcurrentDictionary<Type, IReadOnlyCollection<PropertyInfo>>();

        private static readonly ConcurrentDictionary<Type, string> IdentityColumns =
            new ConcurrentDictionary<Type, string>();

        private static readonly ConcurrentDictionary<Type, string> VersionColumns =
            new ConcurrentDictionary<Type, string>();

        private readonly ConcurrentDictionary<Type, string> _insertSqls = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<Type, string> _purgeSqls = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<Type, string> _deleteSqls = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<Type, string> _selectSqls = new ConcurrentDictionary<Type, string>();
        private readonly ConcurrentDictionary<Type, string> _updateSqls = new ConcurrentDictionary<Type, string>();

        public ReadModelSqlGenerator()
        {
            Configuration = new ReadModelSqlGeneratorConfiguration("[", "]", "[", "]");
        }

        protected ReadModelSqlGenerator(ReadModelSqlGeneratorConfiguration configuration)
        {
            Configuration = configuration;
        }

        public string CreateInsertSql<TReadModel>() where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            string sql;
            if (_insertSqls.TryGetValue(readModelType, out sql))
            {
                return sql;
            }

            var insertColumns = GetInsertColumns<TReadModel>().ToList();

            var columnList = insertColumns.SelectToQuotedColumns(
                    Configuration.ColumnQuotedIdentifierPrefix,
                    Configuration.ColumnQuotedIdentifierSuffix)
                .JoinToSql();
            var parameterList = insertColumns.SelectToSqlParameters().JoinToSql();

            sql = $"INSERT INTO {GetTableName<TReadModel>()} ({columnList}) VALUES ({parameterList})";

            _insertSqls[readModelType] = sql;

            return sql;
        }

        public string CreateSelectSql<TReadModel>() where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            string sql;
            if (_selectSqls.TryGetValue(readModelType, out sql))
            {
                return sql;
            }

            var tableName = GetTableName<TReadModel>();
            var identityColumn = GetIdentityColumn<TReadModel>();

            sql = $"SELECT * FROM {tableName} WHERE {identityColumn} = @EventFlowReadModelId";

            _selectSqls[readModelType] = sql;

            return sql;
        }

        public string CreateDeleteSql<TReadModel>() where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_deleteSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            sql =
                $"DELETE FROM {GetTableName<TReadModel>()} WHERE {Configuration.ColumnQuotedIdentifierPrefix}{GetIdentityColumn<TReadModel>()}{Configuration.ColumnQuotedIdentifierSuffix} = @EventFlowReadModelId";
            _deleteSqls[readModelType] = sql;

            return sql;
        }

        public string CreateUpdateSql<TReadModel>() where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_updateSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            var identityColumn = GetIdentityColumn<TReadModel>();
            var versionColumn = GetVersionColumn<TReadModel>();
            var versionCheck = string.IsNullOrEmpty(versionColumn)
                ? string.Empty
                : $"AND {Configuration.ColumnQuotedIdentifierPrefix}{versionColumn}{Configuration.ColumnQuotedIdentifierSuffix} = @_PREVIOUS_VERSION";

            var updateColumns = GetUpdateColumns<TReadModel>()
                .SelectToUpdateQuotedColumnsByParameters(
                    Configuration.ColumnQuotedIdentifierPrefix,
                    Configuration.ColumnQuotedIdentifierSuffix)
                .JoinToSql();

            var tableName = GetTableName<TReadModel>();

            sql = $"UPDATE {tableName} SET {updateColumns} WHERE {Configuration.ColumnQuotedIdentifierPrefix}{identityColumn}{Configuration.ColumnQuotedIdentifierSuffix} = @{identityColumn} {versionCheck}";

            _updateSqls[readModelType] = sql;

            return sql;
        }

        public string CreatePurgeSql<TReadModel>() where TReadModel : IReadModel
        {
            return _purgeSqls.GetOrCreate(typeof(TReadModel), t => $"DELETE FROM {GetTableName(t)}");
        }

        protected IEnumerable<string> GetInsertColumns<TReadModel>() where TReadModel : IReadModel
        {
            return GetPropertyInfos(typeof(TReadModel)).Select(p => p.Name);
        }

        protected IEnumerable<string> GetUpdateColumns<TReadModel>() where TReadModel : IReadModel
        {
            var identityColumn = GetIdentityColumn<TReadModel>();
            return GetInsertColumns<TReadModel>().Where(c => c != identityColumn);
        }

        public string GetTableName<TReadModel>() where TReadModel : IReadModel
        {
            return GetTableName(typeof(TReadModel));
        }

        protected virtual string GetTableName(Type readModelType)
        {
            return TableNames.GetOrAdd(
                readModelType,
                t =>
                {
                    var qip = Configuration.TableQuotedIdentifierPrefix;
                    var qis = Configuration.TableQuotedIdentifierSuffix;

                    var tableAttribute = t.GetTypeInfo().GetCustomAttribute<TableAttribute>(false);
                    var table = string.IsNullOrEmpty(tableAttribute?.Name)
                        ? $"ReadModel-{t.Name.Replace("ReadModel", string.Empty)}"
                        : tableAttribute.Name;
                    return string.IsNullOrEmpty(tableAttribute?.Schema)
                        ? $"{qip}{table}{qis}"
                        : $"{qip}{tableAttribute?.Schema}{qis}.{qip}{table}{qis}";
                });
        }

        private string GetIdentityColumn<TReadModel>()
        {
            return IdentityColumns.GetOrAdd(
                typeof(TReadModel),
                t =>
                {
                    var propertyInfo = GetPropertyInfos(t)
                        .SingleOrDefault(
                            pi => pi.GetCustomAttributes().Any(a => a is SqlReadModelIdentityColumnAttribute));
                    return propertyInfo?.Name ?? "AggregateId";
                });
        }

        private string GetVersionColumn<TReadModel>()
        {
            return VersionColumns.GetOrAdd(
                typeof(TReadModel),
                t =>
                {
                    var propertyInfo = GetPropertyInfos(t)
                        .SingleOrDefault(
                            pi => pi.GetCustomAttributes().Any(a => a is SqlReadModelVersionColumnAttribute));
                    if (propertyInfo != null)
                    {
                        return propertyInfo.Name;
                    }

                    return GetPropertyInfos(t).Any(n => n.Name == "LastAggregateSequenceNumber")
                        ? "LastAggregateSequenceNumber"
                        : string.Empty;
                });
        }

        protected IReadOnlyCollection<PropertyInfo> GetPropertyInfos(Type readModelType)
        {
            return PropertyInfos.GetOrAdd(
                readModelType,
                t =>
                {
                    return t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => !p.GetCustomAttributes().Any(a => a is SqlReadModelIgnoreColumnAttribute))
                        .OrderBy(p => p.Name)
                        .ToList();
                });
        }
    }
}