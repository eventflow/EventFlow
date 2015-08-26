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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EventFlow.Aggregates
{
    public class GuidIdentity : IIdentityComposer, IIdentityValidator
    {
        private static readonly Regex ValueValidation;

        static GuidIdentity()
        {
            ValueValidation = new Regex(
            @"^[a-z0-9]+\-[a-f0-9]{8}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{12}$",
            RegexOptions.Compiled);
        }

        public string Create(string value) => Guid.NewGuid().ToString();

        public bool IsValid(string context, string value) => !Validate(context, value).Any();

        public IEnumerable<string> Validate(string context, string value)
        {
            var parts = value.Split('-');
            if (parts.Length != 6)
                yield return $"Identity '{value}' in context '{context}' does not appear to be valid";

            var head = parts.Take(1);
            var body = parts.Skip(1).Take(5);

            if (!string.Equals(value.Trim(), value, StringComparison.InvariantCulture))
                yield return $"Identity '{value}' contains leading and/or trailing spaces";
            if (!value.StartsWith(context))
                yield return $"Identity '{value}' does not start with '{context}'";
            if (!ValueValidation.IsMatch(value))
                yield return $"Identity '{value}' does not follow the syntax '[CONTEXT]-[GUID]' in lower case";
        }
    }
}
