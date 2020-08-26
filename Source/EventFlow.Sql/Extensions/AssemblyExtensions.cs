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
using System.IO;
using System.Linq;
using System.Reflection;
using EventFlow.Sql.Migrations;

namespace EventFlow.Sql.Extensions
{
    public static class AssemblyExtensions
    {
        public static IEnumerable<SqlScript> GetEmbeddedSqlScripts(
            this Assembly assembly,
            string startsWith)
        {
            if (string.IsNullOrEmpty(startsWith)) throw new ArgumentNullException(nameof(startsWith));

            var removeFromName = $"{assembly.GetName().Name}.";

            foreach (var manifestResourceName in assembly
                .GetManifestResourceNames()
                .Where(n => n.StartsWith(startsWith))
                .OrderBy(n => n))
            {
                using (var manifestResourceStream = assembly.GetManifestResourceStream(manifestResourceName))
                using (var streamReader = new StreamReader(manifestResourceStream))
                {
                    var name = manifestResourceName.Replace(removeFromName, string.Empty);
                    var content = streamReader.ReadToEnd();

                    yield return new SqlScript(
                        name,
                        content);
                }
            }

        }
    }
}