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
using System.Linq;

namespace EventFlow.Extensions
{
    public static class TypeExtensions
    {
        private static readonly ConcurrentDictionary<Type, string> PrettyPrintCache = new ConcurrentDictionary<Type, string>();

        public static string PrettyPrint(this Type type)
        {
            return PrettyPrintCache.GetOrAdd(
                type,
                t =>
                {
                    try
                    {
                        return PrettyPrintRecursive(t, 0);
                    }
                    catch (Exception)
                    {
                        return t.Name;
                    }
                });
        }

        private static string PrettyPrintRecursive(Type type, int depth)
        {
            if (depth > 3)
            {
                return type.Name;
            }

            var nameParts = type.Name.Split('`');
            if (nameParts.Length == 1)
            {
                return nameParts[0];
            }

            var genericArguments = type.GetGenericArguments();
            return !type.IsConstructedGenericType
                ? $"{nameParts[0]}<{new string(',', genericArguments.Length - 1)}>"
                : $"{nameParts[0]}<{string.Join(",", genericArguments.Select(t => PrettyPrintRecursive(t, depth + 1)))}>";
        }
    }
}