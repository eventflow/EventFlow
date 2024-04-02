// The MIT License (MIT)
// 
// Copyright (c) 2015-2024 Rasmus Mikkelsen
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
            };
        private static readonly ISet<string> CurrentCopyrightHolders = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Rasmus Mikkelsen",
            };
        private static readonly Regex CopyrightLineExtractor = new Regex(
            @"Copyright \(c\) (?<from>20\d{2})\-(?<to>20\d{2}) (?<name>.*)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly int CurrentYear = 2024; // Hardcoded, we don't want test failing every January 1'st

        [Test]
        public async Task EveryFileHasCorrectLicenseHeader()
        {
            var sourceRoot = Path.Combine(Helpers.GetProjectRoot(), "Source");
            var sourceFilesPaths = GetSourceFilePaths(sourceRoot);

            var sourceFiles = await Task.WhenAll(sourceFilesPaths.Select(GetSourceFileAsync));

            // Sanity asserts
            sourceFiles.Should().HaveCountGreaterThan(700);

            // Missing headers
            var missingHeaders = sourceFiles
                .Where(s => s.License.Count < 20)
                .ToList();
            Console.WriteLine("File with missing license header");
            missingHeaders.ForEach(Console.WriteLine);

            // Missing name in header as defined by the CLA (current license)
            var validationErrors = sourceFiles
                .Where(s => s.ValidationErrors.Any())
                .Where(s => !ExternalFiles.Contains(PathRelativeTo(sourceRoot, s.Path)))
                .ToList();
            Console.WriteLine("File with incorrect name in header according to CLA");
            validationErrors.ForEach(Console.WriteLine);

            // Asserts
            missingHeaders.Should().BeEmpty();
            validationErrors.Should().BeEmpty();
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
                .Select(m => new Copyright(
                    m.Groups["name"].Value,
                    (int.Parse(m.Groups["from"].Value), int.Parse(m.Groups["to"].Value)))
                    )
                .ToList();

            return new SourceFile(
                path,
                license,
                copyright);
        }

        private class Copyright
        {
            public string Name { get; }
            public (int, int) Year { get; }

            public bool IsCurrent => CurrentCopyrightHolders.Contains(Name);

            public IEnumerable<string> ValidationErrors()
            {
                if (IsCurrent)
                {
                    if (Year.Item2 != CurrentYear)
                    {
                        yield return $"Year for current copyright holder '{Name}' is not correct, should be {CurrentYear}";
                    }

                    yield break;
                }

                yield return $"Unknown copyright holder '{Name}'";
            }

            public Copyright(
                string name,
                (int, int) year)
            {
                Name = name;
                Year = year;
            }
        }

        private class SourceFile
        {
            public string Path { get; }
            public IReadOnlyCollection<string> License { get; }
            public IReadOnlyCollection<Copyright> Copyright { get; }
            public IReadOnlyCollection<string> ValidationErrors => _validationErrors.Value;

            private readonly Lazy<IReadOnlyCollection<string>> _validationErrors;

            public SourceFile(
                string path,
                IReadOnlyCollection<string> license,
                IReadOnlyCollection<Copyright> copyright)
            {
                Path = path;
                License = license;
                Copyright = copyright;

                _validationErrors = new Lazy<IReadOnlyCollection<string>>(
                        () => Copyright.SelectMany(c => c.ValidationErrors()).ToArray());
            }

            public override string ToString()
            {
                return $"{Path}: {string.Join(", ", ValidationErrors)}";
            }
        }
    }
}
