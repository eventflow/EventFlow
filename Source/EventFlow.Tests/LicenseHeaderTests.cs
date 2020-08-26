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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EventFlow.TestHelpers;
using FluentAssertions;
using NUnit.Framework;

// ReSharper disable StringLiteralTypo

namespace EventFlow.Tests
{
    [Category(Categories.Integration)]
    public class LicenseHeaderTests
    {
        private static readonly char[] LineSplitters = {'\n', '\r'};
        private static readonly ISet<string> ExternalFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                Path.Combine("EventFlow", "Core", "HashHelper.cs"),
                Path.Combine("EventFlow", "Logs", "Internals", "ImportedLibLog.cs")
            };
        private static readonly ISet<string> ValidCopyrightNames = new HashSet<string>
            {
                "Rasmus Mikkelsen",
                "eBay Software Foundation"
            };
        private static readonly Regex CopyrightLineExtractor = new Regex(
            @"Copyright \(c\) 20\d{2}\-20\d{2} (?<name>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Test]
        public async Task EveryFileHasCorrectLicenseHeader()
        {
            var sourceRoot = Path.Combine(Helpers.GetProjectRoot(), "Source");
            var sourceFilesPaths = GetSourceFilePaths(sourceRoot);

            var sourceFiles = await Task.WhenAll(sourceFilesPaths.Select(GetSourceFileAsync));

            // Sanity asserts
            sourceFiles.Should().HaveCountGreaterThan(800);

            // Missing headers
            var missingHeaders = sourceFiles
                .Where(s => s.License.Count < 20)
                .ToList();
            Console.WriteLine("File with missing license header");
            missingHeaders.ForEach(Console.WriteLine);

            // Missing name in header as defined by the CLA (current license)
            var missingNameInHeader = sourceFiles
                .Where(s => !s.Copyright.All(ValidCopyrightNames.Contains))
                .Where(s => !ExternalFiles.Contains(PathRelativeTo(sourceRoot, s.Path)))
                .ToList();
            Console.WriteLine("File with incorrect name in header according to CLA");
            missingNameInHeader.ForEach(Console.WriteLine);

            // Asserts
            missingHeaders.Should().BeEmpty();
            missingNameInHeader.Should().BeEmpty();
        }

        private static string PathRelativeTo(string root, string fullPath)
        {
            var path = fullPath.Replace(root, string.Empty).Trim(Path.DirectorySeparatorChar);
            return path;
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

            var copyright = license
                .Select(l => CopyrightLineExtractor.Match(l))
                .Where(m => m.Success)
                .Select(m => m.Groups["name"].Value)
                .ToList();

            return new SourceFile(
                path,
                license,
                copyright);
        }

        private class SourceFile
        {
            public string Path { get; }
            public IReadOnlyCollection<string> License { get; }
            public IReadOnlyCollection<string> Copyright { get; }

            public SourceFile(
                string path,
                IReadOnlyCollection<string> license,
                IReadOnlyCollection<string> copyright)
            {
                Path = path;
                License = license;
                Copyright = copyright;
            }

            public override string ToString()
            {
                return Path;
            }
        }
    }
}
