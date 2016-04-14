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
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using EventFlow.Core;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests
{
    [Category(Categories.Integration)]
    public class VerifyTests
    {
        private static readonly AppDomainManager AppDomainManager = new AppDomainManager();

        [Test]
        public void VerifyThatAllTestClassesHaveCategoryAssigned()
        {
            var codeBase = ReflectionHelper.GetCodeBase(GetType().Assembly);
            var projectRoot = GetParentDirectories(codeBase).First(d => Directory.GetFiles(d).Any(f => f.EndsWith("README.md")));
            var testAssemblyPaths = GetTestAssembliesFromPath(projectRoot);
            var typesWithMissingCategory = testAssemblyPaths
                .SelectMany(GetTypesFromAssembly)
                .OrderBy(s => s)
                .ToList();
            typesWithMissingCategory.Should().BeEmpty();
        }

        private static readonly Regex RegexTestAssemblies = new Regex(
            @".+\\bin\\(debug|release)\\EventFlow[a-z\.]*\.Tests\.dll$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex RegexTestDirectories = new Regex(
            @".+\\EventFlow[a-z\.]*\.Tests\\bin\\(debug|release)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private static IEnumerable<string> GetTestAssembliesFromPath(string path)
        {
            return Directory.EnumerateDirectories(path, "*.*", SearchOption.AllDirectories)
                .Where(d => RegexTestDirectories.IsMatch(d))
                .SelectMany(Directory.GetFiles)
                .Where(f => RegexTestAssemblies.IsMatch(f));
        } 

        private static IEnumerable<string> GetParentDirectories(string path)
        {
            if (!Directory.Exists(path)) throw new ArgumentException($"Directory '{path}' does not exist!");

            var parent = Directory.GetParent(path);
            while (parent != null)
            {
                yield return parent.FullName;
                parent = parent.Parent;
            }
        }

        private static IReadOnlyCollection<string> GetTypesFromAssembly(string path)
        {
            var appDomainSetup = new AppDomainSetup
                {
                    ConfigurationFile = $"{path}.Config",
                    ShadowCopyFiles = "false",
                    ApplicationBase = Path.GetDirectoryName(path),
                };
            var appDomain = AppDomainManager.CreateDomain(Path.GetFileNameWithoutExtension(path), null, appDomainSetup);

            try
            {
                var typeWithMissingCategoryLister = (TypeWithMissingCategoryLister) appDomain.CreateInstanceAndUnwrap(
                    typeof (TypeWithMissingCategoryLister).Assembly.FullName,
                    typeof (TypeWithMissingCategoryLister).ToString());
                return typeWithMissingCategoryLister.GetTypesWithoutCategoryAttribute(path);
            }
            finally
            {
                AppDomain.Unload(appDomain);
            }
        }
    }
}