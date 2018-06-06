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
using EventFlow.Extensions;
using EventFlow.ReadStores;

namespace EventFlow.Sql.ReadModels
{
    public class ReadModelSqlGenerator : IReadModelSqlGenerator
    {
        private readonly IReadModelAnalyzer _readModelAnalyzer;
        private readonly Dictionary<Type, string> _insertSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _purgeSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _deleteSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _selectSqls = new Dictionary<Type, string>();
        private readonly Dictionary<Type, string> _updateSqls = new Dictionary<Type, string>();

        public ReadModelSqlGenerator(
            IReadModelAnalyzer readModelAnalyzer)
        {
            _readModelAnalyzer = readModelAnalyzer;
        }

        public string CreateInsertSql<TReadModel>()
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_insertSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            var readModelDetails = _readModelAnalyzer.GetReadModelDetails(readModelType);

            sql = string.Format(
                "INSERT INTO {0} ({1}) VALUES ({2})",
                readModelDetails.TableName,
                string.Join(", ", readModelDetails.Fields.Select(f => f.Name)),
                string.Join(", ", readModelDetails.Fields.Select(f => $"@{f.Name}")));
            _insertSqls[readModelType] = sql;

            return sql;
        }

        public string CreateSelectSql<TReadModel>()
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_selectSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            var readModelDetails = _readModelAnalyzer.GetReadModelDetails(readModelType);

            sql = string.Format(
                "SELECT * FROM {0} WHERE {1} = @EventFlowReadModelId",
                readModelDetails.TableName,
                readModelDetails.Fields.Single(f => f.IsIdentity).Name);
            _selectSqls[readModelType] = sql;

            return sql;
        }

        public string CreateDeleteSql<TReadModel>()
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_deleteSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            var readModelDetails = _readModelAnalyzer.GetReadModelDetails(readModelType);

            sql = $"DELETE FROM {readModelDetails.TableName} WHERE {readModelDetails.Fields.Single(f => f.IsIdentity).Name} = @EventFlowReadModelId";
            _deleteSqls[readModelType] = sql;

            return sql;
        }

        public string CreateUpdateSql<TReadModel>()
            where TReadModel : IReadModel
        {
            var readModelType = typeof(TReadModel);
            if (_updateSqls.TryGetValue(readModelType, out var sql))
            {
                return sql;
            }

            var readModelDetails = _readModelAnalyzer.GetReadModelDetails(readModelType);
            sql = string.Format(
                "UPDATE {0} SET {1} WHERE {2} = @{2}",
                readModelDetails.TableName,
                string.Join(", ", readModelDetails.Fields.Where(f => !f.IsIdentity).Select(f => string.Format("{0} = @{0}", f.Name))),
                readModelDetails.Fields.Single(f => f.IsIdentity).Name);
            _updateSqls[readModelType] = sql;

            return sql;
        }

        public string CreatePurgeSql<TReadModel>()
            where TReadModel : IReadModel
        {
            return _purgeSqls.GetOrCreate(
                typeof(TReadModel),
                t =>
                {
                    var readModelDetails = _readModelAnalyzer.GetReadModelDetails(t);
                    return $"DELETE FROM {readModelDetails.TableName}";
                });
        }
    }
}