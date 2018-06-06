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
using System.Text;
using EventFlow.Extensions;
using EventFlow.Sql.ReadModels;

namespace EventFlow.MsSql.ReadStores
{
    public class MssqlReadModelSchemaGenerator : IMssqlReadModelSchemaGenerator
    {
        private readonly IReadModelAnalyzer _readModelAnalyzer;
        private static readonly Dictionary<Type, string> Types = new Dictionary<Type, string>
            {
                {typeof(string), "[nvarchar](MAX)"},
                {typeof(int), "[int]"},
                {typeof(long), "[bigint]"},
                {typeof(bool), "[bit]"},
                {typeof(DateTimeOffset), "[datetimeoffset](7)"}
            };

        public MssqlReadModelSchemaGenerator(
            IReadModelAnalyzer readModelAnalyzer)
        {
            _readModelAnalyzer = readModelAnalyzer;
        }

        public string GetReadModelSchema(Type readModelType)
        {
            var sb = new StringBuilder();
            var details = _readModelAnalyzer.GetReadModelDetails(readModelType);
            var identity = details.Fields.Single(f => f.IsIdentity).Name;

            sb.AppendLine($"CREATE TABLE {details.TableName}");
            sb.AppendLine( "(");
            sb.AppendLine( "   [Id] [bigint] IDENTITY(1,1) NOT NULL,");
            sb.AppendLine($"   [{identity}] [nvarchar](64) NOT NULL,");
            foreach (var f in details.Fields.Where(f => !f.IsIdentity))
            {
                if (!Types.TryGetValue(f.Type, out var t))
                {
                    throw new Exception($"Don't know hot to write '{f.Type.PrettyPrint()}'");
                }

                var required = f.IsRequired ? "NOT " : string.Empty;
                sb.AppendLine($"   [{f.Name}] {t} {required}NULL,");
            }
            sb.AppendLine($"   CONSTRAINT [PK_{details.TableName.Trim('[', ']')}] PRIMARY KEY CLUSTERED");
            sb.AppendLine("   (");
            sb.AppendLine("      [Id] ASC");
            sb.AppendLine("   )");
            sb.AppendLine(")");
            sb.AppendLine();


            sb.AppendLine($"CREATE UNIQUE NONCLUSTERED INDEX [IX_{details.TableName.Trim('[', ']')}_{identity}] ON {details.TableName}");
            sb.AppendLine( "(");
            sb.AppendLine($"   [{identity}] ASC");
            sb.AppendLine( ")");

            return sb.ToString();
        }
    }
}
