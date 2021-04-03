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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using EventFlow.Queries;

namespace EventFlow.Extensions
{
    public static class QueryExtensions
    {
        private static readonly Regex NameRegex = new Regex(
            @"^(Old){0,1}(?<name>[\p{L}\p{Nd}]+?)(V(?<version>[0-9]+)){0,1}$",
            RegexOptions.Compiled);
        public static QueryDefinition GetDefinition(this IQuery query)
        {
            var versionedType = query.GetType();
            var definitionFromAttribute = CreateDefinitionFromAttribute(versionedType).FirstOrDefault();
            return definitionFromAttribute ?? CreateDefinitionFromName(versionedType);
        }


        private static QueryDefinition CreateDefinitionFromName(Type versionedType)
        {
            var match = NameRegex.Match(versionedType.Name);
            if (!match.Success)
            {
                throw new ArgumentException($"Versioned type name '{versionedType.Name}' is not a valid name");
            }

            var version = 1;
            var groups = match.Groups["version"];
            if (groups.Success)
            {
                version = int.Parse(groups.Value);
            }

            var name = match.Groups["name"].Value;
            return new QueryDefinition(
                version,
                versionedType,
                name);
        }

        private static IEnumerable<QueryDefinition> CreateDefinitionFromAttribute(Type versionedType)
        {
            return versionedType
                .GetTypeInfo()
                .GetCustomAttributes()
                .OfType<QueryVersionAttribute>()
                .Select(a => new QueryDefinition(a.Version, versionedType, a.Name));
        }
    }
}
