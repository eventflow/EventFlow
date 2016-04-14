// The MIT License (MIT)
// 
// Copyright (c) 2015-2016 Rasmus Mikkelsen
// Copyright (c) 2015-2016 eBay Software Foundation
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
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;

namespace EventFlow.TestHelpers
{
    public class TypeWithMissingCategoryLister : MarshalByRefObject
    {
        private static readonly ISet<string> ValidCategories;

        static TypeWithMissingCategoryLister()
        {
            ValidCategories = new HashSet<string>(typeof (Categories)
                .GetFields()
                .Select(f => (string) f.GetValue(null)));
        }

        public List<string> GetTypesWithoutCategoryAttribute(string path)
        {
            return AppDomain.CurrentDomain.Load(AssemblyName.GetAssemblyName(path))
                .GetTypes()
                .Where(t => !t.IsAbstract)
                .Where(t => t.GetMethods().Any(mi =>
                    mi.GetCustomAttributes<TestAttribute>().Any() ||
                    mi.GetCustomAttributes<TestCaseAttribute>().Any()))
                .Select(t =>
                    {
                        var categoryAttribute = t.GetCustomAttributes<CategoryAttribute>().SingleOrDefault();
                        if (categoryAttribute == null)
                        {
                            return $"{t.FullName} (no category)";
                        }
                        if (!ValidCategories.Contains(categoryAttribute.Name))
                        {
                            return $"{t.FullName} (invalid category '{categoryAttribute.Name}')";
                        }
                        return null;
                    })
                .Where(e => !string.IsNullOrEmpty(e))
                .ToList();
        }
    }
}