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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using EventFlow.Sql.ReadModels.Attributes;

namespace EventFlow.Sql.ReadModels
{
    // TODO: Find better name!
    public class ReadModelAnalyzer : IReadModelAnalyzer
    {
        private static readonly ConcurrentDictionary<Type, ReadModelDetails> ReadModelDetails = new ConcurrentDictionary<Type, ReadModelDetails>();

        public ReadModelDetails GetReadModelDetails(Type readModelType)
        {
            return ReadModelDetails.GetOrAdd(
                readModelType,
                t =>
                {
                    var fields = (
                        from propertyInfo in GetPropertyInfos(readModelType)
                        let isIdentity = propertyInfo.GetCustomAttributes().Any(a => a is SqlReadModelIdentityColumnAttribute)
                        select new ReadModelField(
                            propertyInfo.Name,
                            propertyInfo.PropertyType,
                            isIdentity)
                        ).ToList();

                    return new ReadModelDetails(
                        GetTableName(readModelType),
                        fields);
                });
        }

        private static string GetTableName(Type readModelType)
        {
            var tableAttribute = readModelType.GetTypeInfo().GetCustomAttribute<TableAttribute>(false);
            return tableAttribute != null
                ? $"[{tableAttribute.Name}]"
                : $"[ReadModel-{readModelType.Name.Replace("ReadModel", string.Empty)}]";
        }

        private static IEnumerable<PropertyInfo> GetPropertyInfos(IReflect readModelType)
        {
            return readModelType
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => !p.GetCustomAttributes().Any(a => a is SqlReadModelIgnoreColumnAttribute))
                .OrderBy(p => p.Name);
        }
    }
}
