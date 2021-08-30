// The MIT License (MIT)
// 
// Copyright (c) 2015-2021 Rasmus Mikkelsen
// Copyright (c) 2015-2021 eBay Software Foundation
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
using EventFlow.Core;
using EventFlow.Extensions;

namespace EventFlow.TestHelpers
{
    public static class SemVerHelper
    {
        public static SemVerReport GenerateReport(Assembly assembly)
        {
            var interfaces = GetInterfaces(assembly.GetTypes().Where(t => t.IsInterface))
                .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();

            return new SemVerReport(
                interfaces);
        }

        private static IEnumerable<SemVerInterface> GetInterfaces(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                var semVerAttribute = type.GetCustomAttribute<SemVerAttribute>();
                if (semVerAttribute == null)
                {
                    continue;
                }

                var methods = GetMethods(type)
                    .OrderBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (!methods.Any())
                {
                    throw new InvalidOperationException($"Interface '{type.PrettyPrint()}' does not have any method");
                }

                yield return new SemVerInterface(
                    type.PrettyPrint(),
                    methods);
            }
        }

        private static IEnumerable<SemVerMethod> GetMethods(Type type)
        {
            foreach (var methodInfo in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                var returnTypeName = methodInfo.ReturnType.PrettyPrint();
                var argumentTypeNames = methodInfo
                    .GetParameters()
                    .Select(p => p.ParameterType.PrettyPrint())
                    .ToArray();

                yield return new SemVerMethod(
                    methodInfo.Name,
                    returnTypeName,
                    argumentTypeNames);
            }
        }

        public class SemVerReport
        {
            public IReadOnlyCollection<SemVerInterface> Interfaces { get; }

            public SemVerReport(
                IReadOnlyCollection<SemVerInterface> interfaces)
            {
                Interfaces = interfaces;
            }
        }

        public class SemVerInterface
        {
            public string Name { get; }
            public IReadOnlyCollection<SemVerMethod> Methods { get; }

            public SemVerInterface(
                string name,
                IReadOnlyCollection<SemVerMethod> methods)
            {
                Name = name;
                Methods = methods;
            }

            public override string ToString()
            {
                return $"{Name} [{Methods.Count}]";
            }
        }

        public class SemVerMethod
        {
            public string Name { get; }
            public string ReturnType { get; }
            public IReadOnlyCollection<string> Arguments { get; }

            public SemVerMethod(
                string name,
                string returnType,
                IReadOnlyCollection<string> arguments)
            {
                Name = name;
                ReturnType = returnType;
                Arguments = arguments;
            }

            public override string ToString()
            {
                return $"{ReturnType} {Name}({string.Join(", ", Arguments)})";
            }
        }
    }
}
