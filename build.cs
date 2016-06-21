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

// IMPORTANT FILES
var FILE_SOLUTIONINFO = System.IO.Path.Combine(PROJECT_DIR, "Source", "SolutionInfo.cs");
var FILE_RELEASENOTES = System.IO.Path.Combine(PROJECT_DIR, "RELEASE_NOTES.md");

// IMPORTANT DIRECTORIES
var DIR_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");

// TOOLS
var TOOL_NUNIT = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "NUnit.Runners", "tools", "nunit-console.exe");
var TOOL_OPENCOVER = System.IO.Path.Combine(PROJECT_DIR, "packages", "test", "OpenCover", "tools", "OpenCover.Console.exe");

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
                        Version = "0.0.1",
                        FileVersion = "0.0.1",
                        InformationalVersion = "0.0.1",
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
            var releaseNotes = ParseReleaseNotes(FILE_RELEASENOTES);

            Information("Version: {0}", releaseNotes.Version);
            Information(string.Join(Environment.NewLine, releaseNotes.Notes));
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