var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath;
var CONFIGURATION = "Release";

// IMPORTANT FILES
var FILE_SOLUTIONINFO = System.IO.Path.Combine(PROJECT_DIR, "Source", "SolutionInfo.cs");

// IMPORTANT DIRECTORIES
var DIR_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");

// TOOLS
var TOOL_NUNIT = System.IO.Path.Combine(PROJECT_DIR, "packages", "build", "NUnit.Runners", "tools", "nunit-console.exe");
var TOOL_OPENCOVER = System.IO.Path.Combine(PROJECT_DIR, "packages", "test", "OpenCover", "tools", "OpenCover.Console.exe");

Console.WriteLine(TOOL_NUNIT);
Console.WriteLine(TOOL_OPENCOVER);

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

Task("Build")
    .IsDependentOn("Version")
    .Does(() =>
{
    BuildProject("Build");
});

Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
{
    ExecuteTest("./**/bin/" + CONFIGURATION + "/EventFlow.Tests.dll", "results");
});

Task("Package")
    .IsDependentOn("Test")
    .Does(() =>
{
});

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
                new NUnitSettings {
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