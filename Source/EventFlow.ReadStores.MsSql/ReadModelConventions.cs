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
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Reflection;
using EventFlow.ReadStores.MsSql.TableGeneration;

namespace EventFlow.ReadStores.MsSql
{
    public class ReadModelConventions : IReadModelConventions
    {
        public string GetTableName<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            var readModelType = typeof(TReadModel);
            var tableAttribute = readModelType.GetCustomAttribute<TableAttribute>();

            return tableAttribute == null
                ? string.Format("ReadModel-{0}", typeof(TReadModel).Name.Replace("ReadModel", string.Empty))
                : tableAttribute.Name;
        }

        public IReadOnlyCollection<ManagedColumn> GetColumns<TReadModel>()
            where TReadModel : IMssqlReadModel
        {
            return typeof(TReadModel)
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(p => p.GetCustomAttribute<KeyAttribute>(true) == null)
                .OrderBy(p => p.Name)
                .Select(p => new ManagedColumn(p.Name, p.PropertyType))
                .ToList();
        }
    }
}
