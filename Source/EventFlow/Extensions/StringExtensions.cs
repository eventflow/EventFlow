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

using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace EventFlow.Extensions
{
    public static class StringExtensions
    {
        private static readonly Regex RegexToSlug = new Regex("(?<=.)([A-Z])", RegexOptions.Compiled);

        public static string ToSlug(this string str)
        {
            return RegexToSlug.Replace(str, "-$0").ToLowerInvariant();
        }

        public static string ToSha256(this string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            using (var sha256 = SHA256.Create())
            {
                var hash = sha256.ComputeHash(bytes);
                return hash
                    .Aggregate(new StringBuilder(), (sb, b) => sb.Append($"{b:x2}"))
                    .ToString();
            }
        }
    }
}