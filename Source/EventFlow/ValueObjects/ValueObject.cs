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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace EventFlow.ValueObjects
{
    public abstract class ValueObject
    {
        private static readonly ConcurrentDictionary<Type, IReadOnlyCollection<FieldInfo>> TypeFields = new ConcurrentDictionary<Type, IReadOnlyCollection<FieldInfo>>();

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj)) return true;
            if (ReferenceEquals(null, obj)) return false;
            var other = obj as ValueObject;
            return other != null && GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
        }

        public override int GetHashCode()
        {
            return GetEqualityComponents().Aggregate(17, (current, obj) => current*23 + (obj != null ? obj.GetHashCode() : 0));
        }

        public static bool operator ==(ValueObject left, ValueObject right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(ValueObject left, ValueObject right)
        {
            return !Equals(left, right);
        }

        public override string ToString()
        {
            return string.Format(
                "{{{0}}}",
                string.Join(", ", GetFields().Select(f => string.Format("{0}: '{1}'", f.Name, f.GetValue(this)))));
        }

        protected virtual IEnumerable<object> GetEqualityComponents()
        {
            return GetFields().Select(x => x.GetValue(this));
        }

        private IEnumerable<FieldInfo> GetFields()
        {
            return TypeFields.GetOrAdd(
                GetType(),
                t =>
                    {
                        var fields = new List<FieldInfo>();
                        while (t != typeof (object) && t != null)
                        {
                            fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));
                            t = t.BaseType;
                        }
                        return fields.OrderBy(f => f.Name).ToList();
                    });
        }
    }
}
