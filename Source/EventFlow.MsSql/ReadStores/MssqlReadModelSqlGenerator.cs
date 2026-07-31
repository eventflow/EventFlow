// The MIT License (MIT)
// 
// Copyright (c) 2015-2025 Rasmus Mikkelsen
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
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using EventFlow.Sql.ReadModels;

namespace EventFlow.MsSql.ReadStores
{
    public class MssqlReadModelSqlGenerator : ReadModelSqlGenerator
    {
        private readonly IMsSqlConfiguration _configuration;
        private readonly ConcurrentDictionary<Type, string> TableNames = new ConcurrentDictionary<Type, string>();
    
        public MssqlReadModelSqlGenerator(IMsSqlConfiguration configuration)
        {
            _configuration = configuration;
        }

        protected override string GetTableName(Type readModelType)
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
                    
                    var schema = string.IsNullOrEmpty(tableAttribute?.Schema)
                        ? _configuration.Schema.Value
                        : tableAttribute.Schema;
                    
                    return $"{qip}{schema}{qis}.{qip}{table}{qis}";
                });
        }
    }
}