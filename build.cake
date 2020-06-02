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
#r "System.Text.RegularExpressions"

#module nuget:?package=Cake.DotNetTool.Module

using System.IO.Compression;
using System.Net;
using System.Xml;
using System.Text.RegularExpressions;
//Can't use argument wit name VERSION due to cake internal usage
var VERSION = Argument<string>("ARG_VERSION", "1.0.1-local");
var NUGET_SOURCE = Argument<string>("ARG_NUGET_SOURCE", "http://nuget.monopoly.su/nuget/Nuget-Push/");
var NUGET_APIKEY = Argument<string>("ARG_NUGET_APIKEY", "Don't push key!");



var PROJECT_DIR = System.IO.Directory.GetCurrentDirectory();
var CONFIGURATION = "Release";

// IMPORTANT DIRECTORIES
var DIR_OUTPUT_PACKAGES = System.IO.Path.Combine(PROJECT_DIR, "Build", "Packages");
var DIR_OUTPUT_REPORTS = System.IO.Path.Combine(PROJECT_DIR, "Build", "Reports");


// IMPORTANT FILES
var FILE_SOLUTION = System.IO.Path.Combine(PROJECT_DIR, "EventFlow.sln");

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
                
            DeleteDirectories(GetDirectories("**/bin"), true);
            DeleteDirectories(GetDirectories("**/obj"), true);
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
        var projects = GetFiles("./**/EventFlow*Tests.csproj");
        foreach(var project in projects)
        {
            DotNetCoreTest(
                project.FullPath,
                new DotNetCoreTestSettings()
                {
                    Configuration = CONFIGURATION,
                    Filter = "Category!=integration",
                    Logger = "TeamCity",
                    TestAdapterPath = ".",
                    NoBuild = true
                });
        }
    });

// =====================================================================================================
Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
        {
            Information("Version: {0}", RELEASE_NOTES.Version);
            Information(string.Join(Environment.NewLine, RELEASE_NOTES.Notes));

            foreach (var project in GetFiles("./Source/**/*.csproj"))
            {
                var name = project.GetDirectory().FullPath;
                var version = VERSION;
                
                if ((name.Contains("Test") && !name.Contains("TestHelpers")) || name.Contains("Example"))
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
Task("LibraryNugetPush")
    .IsDependentOn("Package")
    .WithCriteria(TeamCity.IsRunningOnTeamCity)
    .Does(() => {

        var pushSettings = new DotNetCoreNuGetPushSettings 
        {
            Source = NUGET_SOURCE,
            ApiKey = NUGET_APIKEY,
            ArgumentCustomization = args=>args.Append("--skip-duplicate")
        };
        
        var pkgs = GetFiles($"{DIR_OUTPUT_PACKAGES}\\*.nupkg");
        foreach(var pkg in pkgs) 
        {
                DotNetCoreNuGetPush(pkg.FullPath, pushSettings);  
        }
    });
// =====================================================================================================
Task("TeamCityPublishArtifacts")
    .IsDependentOn("LibraryNugetPush")
    .WithCriteria(TeamCity.IsRunningOnTeamCity)
    .Does(() => {
            //create lightweight html artifact for teamcity
            foreach (var Nuget in GetFiles($"{DIR_OUTPUT_PACKAGES}\\*.nupkg"))
                {   
                string NugetFileName = Nuget.Segments.LastOrDefault();
                //Based on https://github.com/semver/semver/blob/master/semver.md#is-there-a-suggested-regular-expression-regex-to-check-a-semver-string
                Regex NugetRegex = new Regex(@"^(?<PackageName>.*?).(?<PackageVersion>(?<Major>0|[1-9]\d*)\.(?<Minor>0|[1-9]\d*)\.(?<Patch>0|[1-9]\d*)(?:-(?<PreRelease>(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+(?<BuildMetadata>[0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?)\.nupkg$");
                Match Match = NugetRegex.Match(NugetFileName);
                string PackageName = Match.Groups["PackageName"].Value;
                string PackageVersion = Match.Groups["PackageVersion"].Value;
                string HtmlArtifactFileName = PackageName + $".{PackageVersion}.nupkg.html";
                string HtmlArtifactFullName =DIR_OUTPUT_PACKAGES + $"\\{HtmlArtifactFileName}";
                using (System.IO.StreamWriter sw = System.IO.File.CreateText(HtmlArtifactFullName)){    
                 sw.WriteLine($@"
                 <head>
                 <meta http-equiv=""refresh"" content=""1;URL=http://nuget.monopoly.su/feeds/Nuget-Push/{PackageName}/{VERSION}"" />
                 </head>");
                 sw.Close();
                 TeamCity.PublishArtifacts(HtmlArtifactFullName);
                 }
                }
        });

// =====================================================================================================
Task("All")
    .IsDependentOn("TeamCityPublishArtifacts")
    .Does(() =>
        {

        });
// =====================================================================================================



string GetDotNetCoreArgsVersions()
{
    var version = VERSION;
    var versionWithoutPreReleaseTag = version.Split('-')[0];
    return string.Format(
        @"/p:Version={0} /p:AssemblyVersion={1} /p:FileVersion={1} /p:ProductVersion={0}",
        version,versionWithoutPreReleaseTag);
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




TaskSetup(setupContext =>
{
   if (TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.WriteStartBuildBlock(setupContext.Task.Description ?? setupContext.Task.Name);

      TeamCity.WriteStartProgress(setupContext.Task.Description ?? setupContext.Task.Name);
   }
});

TaskTeardown(teardownContext =>
{
   if (TeamCity.IsRunningOnTeamCity)
   {
      TeamCity.WriteEndProgress(teardownContext.Task.Description ?? teardownContext.Task.Name);

      TeamCity.WriteEndBuildBlock(teardownContext.Task.Description ?? teardownContext.Task.Name);
   }
});

RunTarget(Argument<string>("target", "All"));
