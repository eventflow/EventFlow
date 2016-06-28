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

var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath;
var CONFIGURATION = "Release";
var REGEX_NUGETPARSER = new System.Text.RegularExpressions.Regex(
    @"(?<group>[a-z]+)\s+(?<package>[a-z\.0-9]+)\s+\-\s+(?<version>[0-9\.]+)",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

// IMPORTANT FILES
var FILE_SOLUTIONINFO = System.IO.Path.Combine(PROJECT_DIR, "Source", "SolutionInfo.cs");

// IMPORTANT DIRECTORIES
var DIR_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");

// TOOLS
var TOOL_NUNIT = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "NUnit.Runners", "tools", "nunit-console.exe");
var TOOL_OPENCOVER = System.IO.Path.Combine(PROJECT_DIR, "packages", "test", "OpenCover", "tools", "OpenCover.Console.exe");
var TOOL_NUGET = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "NuGet.CommandLine", "tools", "NuGet.exe");
var TOOL_ILMERGE = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "ilmerge", "tools", "ILMerge.exe");
var TOOL_PAKET = System.IO.Path.Combine(PROJECT_DIR, ".paket", "paket.exe");

var VERSION = GetArgumentVersion();
var RELEASE_NOTES = ParseReleaseNotes(System.IO.Path.Combine(PROJECT_DIR, "RELEASE_NOTES.md"));
var NUGET_VERSIONS = GetInstalledNuGetPackages();


// =====================================================================================================
Task("Clean")
    .Does(() =>
        {
            CleanDirectories(new []
                {
                    DIR_PACKAGES,
                    DIR_REPORTS,
                });

            BuildProject("Clean");
        });

// =====================================================================================================
Task("Version")
    .IsDependentOn("Clean")
    .Does(() =>
        {
            CreateAssemblyInfo(
                FILE_SOLUTIONINFO,
                new AssemblyInfoSettings
                    {
                        Version = VERSION.ToString(),
                        FileVersion = VERSION.ToString(),
                        InformationalVersion = VERSION.ToString(),
                        Copyright = string.Format("Copyright (c) Rasmus Mikkelsen 2015 - {0}", DateTime.Now.Year)
                    });
        });

// =====================================================================================================
Task("Build")
    .IsDependentOn("Version")
    .Does(() =>
        {
            BuildProject("Build");
        });

// =====================================================================================================
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
        {
            ExecuteTest("./**/bin/" + CONFIGURATION + "/EventFlow.Tests.dll", "results");
        });

// =====================================================================================================
Task("Package")
    .IsDependentOn("Test")
    .Does(() =>
        {
            Information("Version: {0}", RELEASE_NOTES.Version);
            Information(string.Join(Environment.NewLine, RELEASE_NOTES.Notes));

            ExecuteIlMerge(
                System.IO.Path.Combine(PROJECT_DIR, "Source", "EventFlow", "bin", CONFIGURATION, "EventFlow.dll"),
                System.IO.Path.Combine(PROJECT_DIR, "Source", "EventFlow", "bin", "EventFlow.dll"),
                new []
                    {
                        "Autofac.dll",
                    });

            foreach (var nuspecPath in GetFiles("./Source/**/*.nuspec"))
            {
                ExecutePackage(nuspecPath);
            }
        });

// =====================================================================================================
void BuildProject(string target)
{
    MSBuild(
        "EventFlow.sln",
         s => s
            .WithTarget(target)
            .SetConfiguration(CONFIGURATION)
            .SetMSBuildPlatform(MSBuildPlatform.Automatic)
            .SetVerbosity(Verbosity.Minimal)
            .SetNodeReuse(false)
        );
}

void ExecutePackage(
    FilePath nuspecPath)
{
    Information(nuspecPath.ToString());

    NuGetPack(
        nuspecPath,
        new NuGetPackSettings
            {
                ToolPath = TOOL_NUGET,
                OutputDirectory = DIR_PACKAGES,
                ReleaseNotes = RELEASE_NOTES.Notes.ToList(),
                Version = VERSION.ToString(),
                Verbosity = NuGetVerbosity.Detailed,
            });
}

Version GetArgumentVersion()
{
    var arg = Argument<string>("buildVersion", "0.0.1");
    var version = string.IsNullOrEmpty(arg)
        ? Version.Parse("0.0.1")
        : Version.Parse(arg);

    return version;
}

void ExecuteIlMerge(
    string inputPath,
    string outputPath,
    IEnumerable<string> assemblies)
{
    var baseDir = System.IO.Path.GetDirectoryName(inputPath);
    var assemblyPaths = assemblies
        .Select(a => (FilePath) File(System.IO.Path.Combine(baseDir, a)))
        .ToList();

    ILMerge(
        outputPath,
        inputPath,
        assemblyPaths,
        new ILMergeSettings
            {
                Internalize = true,
                ArgumentCustomization = aggs => aggs.Append("/targetplatform:v4 /allowDup /target:library"),
                ToolPath = TOOL_ILMERGE,
            });
}

IReadOnlyDictionary<string, string> GetInstalledNuGetPackages()
{
    var nugetPackages = new Dictionary<string, string>();
    var paketOutput = ExecuteCommand(TOOL_PAKET, "show-installed-packages");

    var match = REGEX_NUGETPARSER.Match(paketOutput);
    if (!match.Success)
    {
        throw new Exception("Unable to read NuGet package versions");
    }

    do
    {
        var package = match.Groups["package"].Value;
        var version = match.Groups["version"].Value;

        Information("NuGet package '{0}' is version '{1}'", package, version);

        nugetPackages.Add(package, version);

    } while((match = match.NextMatch()) != null && match.Success);

    return nugetPackages;
}

string ExecuteCommand(string exePath, string arguments = null)
{
    Information("Executing '{0}' {1}", exePath, arguments ?? string.Empty);

    using (var process = new System.Diagnostics.Process())
    {
        process.StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                FileName = exePath,
                Arguments = arguments,
            };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();

        if (!process.WaitForExit(30000))
        {
            throw new Exception("Failed to stop process!");
        }

        Debug(output);

        return output;
    }
}

void ExecuteTest(string files, string reportName)
{
    var openCoverReportPath = System.IO.Path.Combine(DIR_REPORTS, "opencover-" + reportName + ".xml");
    var nunitOutputPath = System.IO.Path.Combine(DIR_REPORTS, "nunit-" + reportName + ".txt");
    var nunitResultsPath = System.IO.Path.Combine(DIR_REPORTS, "nunit-" + reportName + ".xml");

    OpenCover(tool =>
        {
            tool.NUnit(
                files,
                new NUnitSettings
                    {
                        ShadowCopy = false,
                        Timeout = 30000,
                        NoLogo = true,
                        Framework = "net-4.5.1",
                        ToolPath = TOOL_NUNIT,
                        OutputFile = nunitOutputPath,
                        ResultsFile = nunitResultsPath,
                    });
        },
    new FilePath(openCoverReportPath),
    new OpenCoverSettings
        {
            ToolPath = TOOL_OPENCOVER,
            ArgumentCustomization = aggs => aggs.Append("-returntargetcode")
        }
        .WithFilter("+[EventFlow*]*")
        .WithFilter("-[*Tests]*")
        .WithFilter("-[*TestHelpers]*")
        .WithFilter("-[*Shipping*]*"));
}

RunTarget("Package");