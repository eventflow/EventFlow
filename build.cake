// The MIT License (MIT)
// 
// Copyright (c) 2015-2018 Rasmus Mikkelsen
// Copyright (c) 2015-2018 eBay Software Foundation
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
#r "System.Xml"

#module nuget:?package=Cake.DotNetTool.Module
#tool "nuget:?package=OpenCover"
#tool "dotnet:?package=sourcelink"

using System.IO.Compression;
using System.Net;
using System.Xml;

var VERSION = GetArgumentVersion();
var PROJECT_DIR = Context.Environment.WorkingDirectory.FullPath;
var CONFIGURATION = "Release";

// IMPORTANT DIRECTORIES
var DIR_OUTPUT_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_OUTPUT_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");

// IMPORTANT FILES
var FILE_OPENCOVER_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "opencover-results.xml");
var FILE_NUNIT_XML_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "nunit-results.xml");
var FILE_NUNIT_TXT_REPORT = System.IO.Path.Combine(DIR_OUTPUT_REPORTS, "nunit-output.html");
var FILE_SOLUTION = System.IO.Path.Combine(PROJECT_DIR, "EventFlow.sln");
var FILE_RUNSETTINGS = System.IO.Path.Combine(PROJECT_DIR, "Test.runsettings");
var RELEASE_NOTES = ParseReleaseNotes(System.IO.Path.Combine(PROJECT_DIR, "RELEASE_NOTES.md"));

// =====================================================================================================
Task("Default")
    .IsDependentOn("Package");

// =====================================================================================================
Task("Clean")
    .Does(() =>
        {
            CleanDirectories(new []
                {
                    DIR_OUTPUT_PACKAGES,
                    DIR_OUTPUT_REPORTS,
                });
				
            var settings = new DeleteDirectorySettings {Force = true, Recursive = true};

			DeleteDirectories(GetDirectories("**/bin"), settings);
			DeleteDirectories(GetDirectories("**/obj"), settings);
        });
	
// =====================================================================================================
Task("Restore")
    .IsDependentOn("Clean")
    .Does(() =>
        {
			DotNetCoreRestore(
				".", 
				new DotNetCoreRestoreSettings()
				{
					ArgumentCustomization = aggs => aggs.Append(GetDotNetCoreArgsVersions())
				});
        });
		
// =====================================================================================================
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
        {
            DotNetCoreBuild(
				".", 
				new DotNetCoreBuildSettings()
				{
                    NoRestore = true,
					Configuration = CONFIGURATION,
					ArgumentCustomization = aggs => aggs
                        .Append(GetDotNetCoreArgsVersions())
                        .Append("/p:ci=true")
                        .Append("/p:SourceLinkEnabled=true")
                        .Append("/p:TreatWarningsAsErrors=true")
				});
        });

// =====================================================================================================
Task("Test")
    .IsDependentOn("Build")
    .Does(() =>
        {
            ExecuteTest(FindTestDlls("net472"));
            ExecuteTest(FindTestDlls("netcoreapp3.1"));
        })
	.Finally(() =>
        {
            UploadArtifact(FILE_NUNIT_TXT_REPORT);
        });

// =====================================================================================================
Task("Package")
    .IsDependentOn("Test")
    .Does(() =>
        {
            Information("Version: {0}", RELEASE_NOTES.Version);
            Information(string.Join(Environment.NewLine, RELEASE_NOTES.Notes));

			foreach (var project in GetFiles("./Source/**/*.csproj"))
			{
				var name = project.GetDirectory().FullPath;
				var version = VERSION.ToString();
				
				if ((name.Contains("Test") && !name.Contains("TestHelpers")) 
                    || name.Contains("Example")
                    || name.Contains("CodeStyle"))
				{
					continue;
				}

                SetReleaseNotes(project.ToString());
							
				DotNetCorePack(
					name,
					new DotNetCorePackSettings()
					{
						Configuration = CONFIGURATION,
						OutputDirectory = DIR_OUTPUT_PACKAGES,
						NoBuild = true,
						ArgumentCustomization = aggs => aggs.Append(GetDotNetCoreArgsVersions())
					});
			}
        });

// =====================================================================================================
Task("ValidateSourceLink")
    .IsDependentOn("Package")
    .Does(() =>
        {
            var files = GetFiles($"./Build/Packages/EventFlow*.nupkg");
            if (!files.Any())
            {
                throw new Exception("No NuGet packages found!");
            }

            foreach(var file in files)
            {
                var filePath = $"{file}".Replace("/", "\\");		   
                Information("Validating SourceLink for NuGet file: {0}", filePath);
                ExecuteCommand("sourcelink", $"test {filePath}");
            }
        });

// =====================================================================================================
Task("All")
    .IsDependentOn("Package")
    //.IsDependentOn("ValidateSourceLink") builds on AppVeyor fail for some unknown reason
    .Does(() =>
        {
        });

// =====================================================================================================

Version GetArgumentVersion()
{
    return Version.Parse(EnvironmentVariable("APPVEYOR_BUILD_VERSION") ?? "0.0.1");
}

string GetDotNetCoreArgsVersions()
{
	var version = GetArgumentVersion().ToString();
	
	return string.Format(
		@"/p:Version={0} /p:AssemblyVersion={0} /p:FileVersion={0} /p:ProductVersion={0}",
		version);
}

void SetReleaseNotes(string filePath)
{
    var releaseNotes = string.Join(Environment.NewLine, RELEASE_NOTES.Notes);

    SetXmlNode(
        filePath,
        "Project/PropertyGroup/PackageReleaseNotes",
        releaseNotes);
}

void SetXmlNode(string filePath, string xmlPath, string content)
{
    var xmlDocument = new XmlDocument();
    xmlDocument.Load(filePath);

    var node = xmlDocument.SelectSingleNode(xmlPath) as XmlElement;
    if (node == null)
    {
        throw new Exception($"Project {filePath} does not have a {xmlPath} property");
    }

    if (!AppVeyor.IsRunningOnAppVeyor)
    {
        Information($"Skipping update {xmlPath} in {filePath}");
        return;
    } 
    else
    {
        Information($"Setting {xmlPath} in {filePath}");
        
        node.InnerText = content;

        xmlDocument.Save(filePath);
    }
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

IEnumerable<FilePath> FindTestDlls(string framework)
{
    var dirs = GetDirectories($"Source/*.Tests/bin/{CONFIGURATION}/{framework}");
    var cwd = Context.Environment.WorkingDirectory;
    var files = dirs
        .Select(cwd.GetRelativePath)
        .Select(d => d.Segments[1])
        .Select(name => $"Source/{name}/bin/{CONFIGURATION}/{framework}/{name}.dll")
        .Select(file => new FilePath(file));
    return files;
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

        if (process.ExitCode != 0)
        {
            Error(output);
            throw new Exception(string.Format("Error code {0} was returned", process.ExitCode));
        }

        Debug(output);

        return output;
    }
}

void ExecuteTest(IEnumerable<FilePath> paths)
{
	OpenCover(tool => 
		{
            var settings = new DotNetCoreVSTestSettings()
                {
                    Parallel = true,
                    ToolTimeout = TimeSpan.FromMinutes(30),
                    Settings = FILE_RUNSETTINGS,
                    ResultsDirectory = DIR_OUTPUT_REPORTS,
                    ArgumentCustomization = args =>
                        args.Append("--nologo")
                };

            tool.DotNetCoreVSTest(paths, settings);
        },
        new FilePath(FILE_OPENCOVER_REPORT),
        new OpenCoverSettings
            {
                Register = AppVeyor.IsRunningOnAppVeyor ? "appveyor" : "user",
                ReturnTargetCodeOffset = 1000,
                MergeOutput = true,
                LogLevel = OpenCoverLogLevel.Warn,
            }
            .WithFilter("+[EventFlow*]*")
            .WithFilter("-[*Tests]*")
            .WithFilter("-[*TestHelpers]*")
            .WithFilter("-[*Shipping*]*"));
}

RunTarget(Argument<string>("target", "Package"));
