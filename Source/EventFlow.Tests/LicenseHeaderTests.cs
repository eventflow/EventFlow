// The MIT License (MIT)
// 
// Copyright (c) 2015-2019 Rasmus Mikkelsen
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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using NUnit.Framework;

namespace EventFlow.Tests
{
    public class LicenseHeaderTests
    {
        private static readonly char[] LineSplitters = {'\n', '\r'};

        [Test]
        public async Task EveryFileHasLicenseHeader()
        {
            var projectRoot = Helpers.GetProjectRoot();
            var sourceFilesPaths = GetSourceFilePaths(projectRoot);

            var sourceFiles = await Task.WhenAll(sourceFilesPaths.Select(GetSourceFileAsync));

            var missingHeaders = sourceFiles
                .Where(s => s.License.Count < 20)
                .ToList();
            
            missingHeaders.ForEach(Console.WriteLine);

            missingHeaders.Should().BeEmpty();
        }

        private static IEnumerable<string> GetSourceFilePaths(
            string directory)
        {
            var objDir = $"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}";
            return Directory.EnumerateFiles(directory, "*.cs", SearchOption.AllDirectories)
                .Where(p => !p.Contains(objDir));
        }

        private static async Task<SourceFile> GetSourceFileAsync(string path)
        {
            if (!File.Exists(path))
            {
                throw new ArgumentException($"File not found: {path}", nameof(path));
            }

            string content;
            using (var file = File.OpenText(path))
            {
                content = await file.ReadToEndAsync();
            }

            var license = content
                .Split(LineSplitters, StringSplitOptions.RemoveEmptyEntries)
                .TakeWhile(l => l.StartsWith("//"))
                .ToList();

            return new SourceFile(
                path,
                license);
        }

        private class SourceFile
        {
            public string Path { get; }
            public IReadOnlyCollection<string> License { get; }

            public SourceFile(
                string path,
                IReadOnlyCollection<string> license)
            {
                Path = path;
                License = license;
            }

            public override string ToString()
            {
                return Path;
            }
        }
    }
}
