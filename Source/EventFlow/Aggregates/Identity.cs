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
                var value = $"{Name}-{Guid.NewGuid()}".ToLowerInvariant();
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
                yield return $"Aggregate ID of type '{typeof (T).Name}' is null or empty";
                yield break;
            }

            if (!string.Equals(value.Trim(), value, StringComparison.InvariantCulture))
                yield return$"Aggregate ID '{value}' of type '{typeof (T).Name}' contains leading and/or trailing spaces";
            if (!value.StartsWith(Name))
                yield return $"Aggregate ID '{value}' of type '{typeof (T).Name}' does not start with '{Name}'";
            if (!ValueValidation.IsMatch(value))
                yield return $"Aggregate ID '{value}' of type '{typeof (T).Name}' does not follow the syntax '[NAME]-[GUID]' in lower case";
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

    // NOTE work in progress
    public abstract class Identity<TIdentity, TComposer, TValidator> : SingleValueObject<string>, IIdentity
        where TIdentity : Identity<TIdentity, TComposer, TValidator>
        where TComposer : IIdentityComposer,  new()
        where TValidator : IIdentityValidator, new()
    {
        private static readonly string Name;
        private static readonly Regex NameReplace;
        private static readonly TValidator Validator;
        private static readonly TComposer Composer;

        static Identity()
        {
            NameReplace = new Regex("Id$", RegexOptions.Compiled);
            Name = NameReplace.Replace(typeof(TIdentity).Name, string.Empty).ToLowerInvariant();
            Validator = new TValidator();
            Composer = new TComposer();
        }

        protected Identity(string value)
            : base(value)
        {
            var validationErrors = Validator.Validate(Name, value);
            if (validationErrors.Any())
            {
                throw new ArgumentException(string.Format(
                    "Aggregate ID is invalid: {0}",
                    string.Join(", ", validationErrors)));
            }
        }

        public static bool IsValid(string value)
        {
            var identity = With(value);
            return Validator.IsValid(Name, value);
        }

        public static TIdentity New(string value = null)
        {
            var qualifiedValue = string.Format("{0}-{1}", Name, Composer.Create(value)).ToLowerInvariant();
            return With(qualifiedValue);
        }

        public static IEnumerable<string> Validate(string value)
        {
            var identity = With(value);
            return Validator.Validate(Name, value);
        }

        public static TIdentity With(string value)
        {
            return (TIdentity)Activator.CreateInstance(typeof(TIdentity), value);
        }
    }
}
