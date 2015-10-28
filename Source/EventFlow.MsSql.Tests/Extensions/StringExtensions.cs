// The MIT License (MIT)
// 
// Copyright (c) 2015 Rasmus Mikkelsen
// Copyright (c) 2015 eBay Software Foundation
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
// 
using System;
using System.Text.RegularExpressions;

namespace EventFlow.MsSql.Tests.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex DatabaseReplace = new Regex(@"(?<key>Initial Catalog|Database)=[a-zA-Z0-9\-_]+", RegexOptions.Compiled);
        private static readonly Regex DatabaseExtract = new Regex(@"(Initial Catalog|Database)=(?<database>[a-zA-Z0-9\-_]+)", RegexOptions.Compiled);

        public static string GetDatabaseInConnectionstring(this string connectionString)
        {
            var match = DatabaseExtract.Match(connectionString);
            if (!match.Success)
            {
                throw new ArgumentException(string.Format(
                    "Could not get database from connection string '{0}'",
                    connectionString));
            }

            return match.Groups["database"].Value;
        }

        public static string ReplaceDatabaseInConnectionstring(this string connectionString, string database)
        {
            return DatabaseReplace.Replace(connectionString, $"${{key}}={database}");
        }
    }
}
