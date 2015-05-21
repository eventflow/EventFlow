﻿// The MIT License (MIT)
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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using EventFlow.Extensions;

namespace EventFlow.ReadStores.MsSql
{
    public class ReadModelSqlGenerator : IReadModelSqlGenerator
    {
        private readonly Dictionary<Type, string> _insertSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _selectSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _updateSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _purgeSqls = new Dictionary<Type, string>(); 
        private static readonly ConcurrentDictionary<Type, string> TableNames = new ConcurrentDictionary<Type, string>();

        public string CreateInsertSql<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            var readModelType = typeof(TReadModel);
            string sql;
            if (_insertSqls.TryGetValue(readModelType, out sql))
            {
                return sql;
            }

            sql = string.Format(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                GetTableName<TReadModel>(),
                string.Join(", ", GetInsertColumns<TReadModel>()),
                string.Join(", ", GetInsertColumns<TReadModel>().Select(c => string.Format("@{0}", c))));
            _insertSqls[readModelType] = sql;

            return sql;
        }

        public string CreateSelectSql<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            var readModelType = typeof (TReadModel);
            string sql;
            if (_selectSqls.TryGetValue(readModelType, out sql))
            {
                return sql;
            }

            sql = string.Format("SELECT * FROM {0} WHERE AggregateId = @AggregateId", GetTableName<TReadModel>());
            _selectSqls[readModelType] = sql;

            return sql;
        }

        public string CreateUpdateSql<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            var readModelType = typeof (TReadModel);
            string sql;
            if (_updateSqls.TryGetValue(readModelType, out sql))
            {
                return sql;
            }

            sql = string.Format(
                "UPDATE {0} SET {1} WHERE AggregateId = @AggregateId",
                GetTableName<TReadModel>(),
                string.Join(", ", GetUpdateColumns<TReadModel>().Select(c => string.Format("{0} = @{0}", c))));
            _updateSqls[readModelType] = sql;

            return sql;
        }

        public string CreatePurgeSql<TReadModel>()
            where TReadModel : IReadModel
        {
            return _purgeSqls.GetOrCreate(
                typeof (TReadModel),
                t => string.Format("DELETE FROM {0}", GetTableName(t)));
        }

        protected IEnumerable<string> GetInsertColumns<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            return typeof(TReadModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.Name != "Id") // TODO: Maybe use the key attribute to mark this
                .OrderBy(p => p.Name)
                .Select(p => p.Name);
        }

        protected IEnumerable<string> GetUpdateColumns<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            return GetInsertColumns<TReadModel>()
                .Where(c => c != "AggregateId");
        }

        public string GetTableName<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            return GetTableName(typeof(TReadModel));
        }

        protected virtual string GetTableName(Type readModelType)
        {
            return TableNames.GetOrAdd(
                readModelType,
                t =>
                    {
                        var tableAttribute = t.GetCustomAttribute<TableAttribute>(false);
                        return tableAttribute != null
                            ? string.Format("[{0}]", tableAttribute.Name)
                            : string.Format("[ReadModel-{0}]", t.Name.Replace("ReadModel", string.Empty));
                    });
        }
    }
}
