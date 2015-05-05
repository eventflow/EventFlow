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
using EventFlow.ValueObjects;

namespace EventFlow.Aggregates
{
    public abstract class Identity<T> : SingleValueObject<string>, IIdentity
        where T : Identity<T>
    {
        // ReSharper disable StaticMemberInGenericType
        private static readonly Regex NameReplace = new Regex("Id$", RegexOptions.Compiled);
        private static readonly string Name = NameReplace.Replace(typeof (T).Name, string.Empty).ToLowerInvariant();
        private static readonly Regex ValueValidation = new Regex(
            @"^[a-z0-9]+\-[a-f0-9]{8}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{4}\-[a-f0-9]{12}$",
            RegexOptions.Compiled);
        // ReSharper enable StaticMemberInGenericType

        public static T New
        {
            get
            {
                var value = string.Format("{0}-{1}", Name, Guid.NewGuid()).ToLowerInvariant();
                return With(value);
            }
        }

        public static T With(string value)
        {
            return (T)Activator.CreateInstance(typeof(T), value);
        }

        public static bool IsValid(string value)
        {
            return !Validate(value).Any();
        }

        public static IEnumerable<string> Validate(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                yield return string.Format("Aggregate ID of type '{0}' is null or empty", typeof(T).Name);
                yield break;
            }

            if (!string.Equals(value.Trim(), value, StringComparison.InvariantCulture))
                yield return string.Format("Aggregate ID '{0}' of type '{1}' contains leading and/or traling spaces", value, typeof(T).Name);
            if (!value.StartsWith(Name))
                yield return string.Format("Aggregate ID '{0}' of type '{1}' does not start with '{2}'", value, typeof(T).Name, Name);
            if (!ValueValidation.IsMatch(value))
                yield return string.Format("Aggregate ID '{0}' of type '{1}' does not follow the syntax '[NAME]-[GUID]' in lower case", value, typeof(T).Name);
        }

        protected Identity(string value) : base(value)
        {
            var validationErrors = Validate(value).ToList();
            if (validationErrors.Any())
            {
                throw new ArgumentException(string.Format(
                    "Aggregate ID is invalid: {0}",
                    string.Join(", ", validationErrors)));
            }
        }
    }
}
