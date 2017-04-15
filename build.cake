// The MIT License (MIT)
// 
// Copyright (c) 2015-2017 Rasmus Mikkelsen
// Copyright (c) 2015-2017 eBay Software Foundation
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
// 

#r "System.IO.Compression.FileSystem"

#tool "nuget:?package=gitlink"

using System.Net;
using System.IO.Compression;

var VERSION = GetArgumentVersion();
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath;
var CONFIGURATION = "Release";
var REGEX_NUGETPARSER = new System.Text.RegularExpressions.Regex(
    @"(?<group>[a-z]+)\s+(?<package>[a-z\.0-9]+)\s+\-\s+(?<version>[0-9\.]+)",
    System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.Compiled);

// IMPORTANT DIRECTORIES
var DIR_OUTPUT_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_OUTPUT_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");
var DIR_OUTPUT_DOCUMENTATION = System.IO.Path.Combine(PROJECT_DIR, "Build", "Documentation");
var DIR_DOCUMENTATION = System.IO.Path.Combine(PROJECT_DIR, "Documentation");
var DIR_BUILT_DOCUMENTATION = System.IO.Path.Combine(DIR_DOCUMENTATION, "_build");
var DIR_BUILT_HTML_DOCUMENTATION = System.IO.Path.Combine(DIR_BUILT_DOCUMENTATION, "html");
var DIR_SOURCE = System.IO.Path.Combine(PROJECT_DIR, "Source");

// IMPORTANT FILES
var FILE_SOLUTIONINFO = System.IO.Path.Combine(PROJECT_DIR, "Source", "SolutionInfo.cs");
var FILE_OPENCOVER_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "opencover-results.xml");
var FILE_NUNIT_XML_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "nunit-results.xml");
var FILE_NUNIT_TXT_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "nunit-output.txt");
var FILE_DOCUMENTATION_MAKE = System.IO.Path.Combine(DIR_DOCUMENTATION, "make.bat");
var FILE_SOLUTION = System.IO.Path.Combine(PROJECT_DIR, "EventFlow.sln");
var FILE_OUTPUT_DOCUMENTATION_ZIP = System.IO.Path.Combine(
    DIR_OUTPUT_DOCUMENTATION,
    string.Format("EventFlow-HtmlDocs-v{0}.zip", VERSION));

// TOOLS
var TOOL_NUNIT = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "NUnit.ConsoleRunner", "tools", "nunit3-console.exe");
var TOOL_OPENCOVER = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "OpenCover", "tools", "OpenCover.Console.exe");
var TOOL_PAKET = System.IO.Path.Combine(PROJECT_DIR, ".paket", "paket.exe");
var TOOL_GITVERSION = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "GitVersion.CommandLine", "tools", "GitVersion.exe");

var RELEASE_NOTES = ParseReleaseNotes(System.IO.Path.Combine(PROJECT_DIR, "RELEASE_NOTES.md"));


// =====================================================================================================
Task("Clean")
    .Does(() =>
        {
            CleanDirectories(new []
                {
                    DIR_OUTPUT_PACKAGES,
                    DIR_OUTPUT_REPORTS,
                    DIR_OUTPUT_DOCUMENTATION,
                    DIR_BUILT_DOCUMENTATION,
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
                        Company = "Rasmus Mikkelsen",
                        Copyright = string.Format("Copyright (c) Rasmus Mikkelsen 2015 - {0} (SHA:{1})", DateTime.Now.Year, GetSha()),
                        Configuration = CONFIGURATION,
                        Trademark = "",
                        Product = "EventFlow",
                        ComVisible = false,
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
    .Finally(() => 
        {
            UploadArtifact(FILE_NUNIT_TXT_REPORT);
            UploadTestResults(FILE_NUNIT_XML_REPORT);
        })
    .Does(() =>
        {
            ExecuteTest("./Source/**/bin/" + CONFIGURATION + "/EventFlow*Tests.dll", FILE_NUNIT_XML_REPORT);
        });

// =====================================================================================================
Task("Package")
    .IsDependentOn("Test")
    .Does(() =>
        {
            Information("Version: {0}", RELEASE_NOTES.Version);
            Information(string.Join(Environment.NewLine, RELEASE_NOTES.Notes));

            Information("Updating PDB files using GitLink");
            GitLink(
                DIR_SOURCE,
                new GitLinkSettings{
                    RepositoryUrl = "https://github.com/eventflow/EventFlow",
                    SolutionFileName = FILE_SOLUTION
                });

            Information("Paket pack");
            ExecuteCommand(TOOL_PAKET, string.Format(
                "pack pin-project-references output \"{0}\" buildconfig {1} releaseNotes \"{2}\"",
                DIR_OUTPUT_PACKAGES,
                CONFIGURATION,
                string.Join(Environment.NewLine, RELEASE_NOTES.Notes)));
        });

// =====================================================================================================
Task("Documentation")
    .IsDependentOn("Clean")
    .Does(() =>
        {
            ExecuteCommand(FILE_DOCUMENTATION_MAKE, "html", DIR_DOCUMENTATION);

            ZipFile.CreateFromDirectory(DIR_BUILT_HTML_DOCUMENTATION, FILE_OUTPUT_DOCUMENTATION_ZIP);
        });

// =====================================================================================================
Task("All")
    .IsDependentOn("Package")
    .IsDependentOn("Documentation")
    .Does(() =>
        {

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
            .UseToolVersion(MSBuildToolVersion.VS2017)
        );
}

Version GetArgumentVersion()
{
    var arg = Argument<string>("buildVersion", "0.0.1");
    var version = string.IsNullOrEmpty(arg)
        ? Version.Parse("0.0.1")
        : Version.Parse(arg);

    return version;
}

string GetSha()
{
    return AppVeyor.IsRunningOnAppVeyor
        ? string.Format("git sha: {0}", GitVersion(new GitVersionSettings { ToolPath = TOOL_GITVERSION, }).Sha)
        : "developer build";
}

void UploadArtifact(string filePath)
{
    if (!FileExists(filePath))
    {
        Information("Skipping uploading of artifact, does not exist: {0}", filePath);
        return;
    }

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading artifact: {0}", filePath);

        AppVeyor.UploadArtifact(filePath);
    }
    else
    {
        Information("Not on AppVeyor, skipping artifact upload of: {0}", filePath);
    }
}

void UploadTestResults(string filePath)
{
    if (!FileExists(filePath))
    {
        Information("Skipping uploading of test results, does not exist: {0}", filePath);
        return;
    }

    if (AppVeyor.IsRunningOnAppVeyor)
    {
        Information("Uploading test results: {0}", filePath);

        try
        {
            using (var webClient = new WebClient())
            {
                webClient.UploadFile(
                    string.Format(
                        "https://ci.appveyor.com/api/testresults/nunit3/{0}",
                        Environment.GetEnvironmentVariable("APPVEYOR_JOB_ID")),
                    filePath);
            }
        }
        catch (Exception e)
        {
            Error(
                "Failed to upload '{0}' due to {1} - {2}: {3}",
                filePath,
                e.Message,
                e.GetType().Name,
                e.ToString());
        }
        
        /*
        // This should work, but doesn't seem to
        AppVeyor.UploadTestResults(
            filePath,
            AppVeyorTestResultsType.NUnit3);*/
    }    
    else
    {
        Information("Not on AppVeyor, skipping test result upload of: {0}", filePath);
    }
}

string ExecuteCommand(string exePath, string arguments = null, string workingDirectory = null)
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
                WorkingDirectory = workingDirectory,
            };
        process.Start();

        var output = process.StandardOutput.ReadToEnd();

        if (!process.WaitForExit(30000))
        {
            throw new Exception("Failed to stop process!");
        }

        Debug(output);

        if (process.ExitCode != 0)
        {
            throw new Exception(string.Format("Error code {0} was returned", process.ExitCode));
        }

        return output;
    }
}

void ExecuteTest(string files, string resultsFile)
{

    OpenCover(tool =>
        {
            tool.NUnit3(
                files,
                new NUnit3Settings
                    {
                        ShadowCopy = false,
                        Timeout = 600000,
                        NoHeader = true,
                        NoColor = true,
                        Framework = "net-4.5",
                        ToolPath = TOOL_NUNIT,
                        OutputFile = FILE_NUNIT_TXT_REPORT,
                        Results = resultsFile,
                        DisposeRunners = true
                    });
        },
    new FilePath(FILE_OPENCOVER_REPORT),
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

RunTarget(Argument<string>("target", "Package"));
