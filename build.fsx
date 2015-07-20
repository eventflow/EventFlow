#r @"tools\FAKE.Core\tools\FakeLib.dll"
open System
open Fake 
open Fake.AssemblyInfoFile

let releaseNotes = 
    ReadFile "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let buildVersion = getBuildParamOrDefault "buildVersion" "0.0.1"
let nugetApikey = getBuildParamOrDefault "nugetApikey" ""

let dirPackages = "./Build/Packages"
let dirReports = "./Build/Reports"
let filePathUnitTestReport = dirReports + "/NUnit.xml"
let fileListUnitTests = !! ("**/bin/" @@ buildMode @@ "/EventFlow*Tests.dll")
let toolNUnit = "./Tools/NUnit.Runners/tools"
let toolIlMerge = "./Tools/ilmerge/tools/ILMerge.exe"
let nugetVersion = buildVersion // + "-alpha"
let nugetVersionDep = "["+nugetVersion+"]"


Target "Clean" (fun _ ->
    CleanDirs [ dirPackages; dirReports ]
    )

Target "SetVersion" (fun _ ->
    CreateCSharpAssemblyInfo "./Source/SolutionInfo.cs"
        [Attribute.Version buildVersion
         Attribute.InformationalVersion nugetVersion
         Attribute.FileVersion buildVersion]
    )

Target "BuildApp" (fun _ ->
    MSBuild null "Build" ["Configuration", buildMode] ["./EventFlow.sln"]
    |> Log "AppBuild-Output: "
    )

Target "UnitTest" (fun _ ->
    fileListUnitTests
        |> NUnit (fun p -> 
            {p with
                DisableShadowCopy = true;
                Framework = "net-4.0";
                ToolPath = "./Tools/NUnit.Runners/tools";
                ToolName = "nunit-console-x86.exe";
                OutputFile = filePathUnitTestReport})
    )

Target "CreatePackageEventFlow" (fun _ ->
    let binDir = "Source\\EventFlow\\bin\\" + buildMode + "\\"
    let result = ExecProcess (fun info ->
       info.Arguments <- "/targetplatform:v4 /internalize /allowDup /target:library /out:Source\\EventFlow\\bin\\EventFlow.dll " + binDir + "EventFlow.dll " + binDir + "Autofac.dll"
       info.FileName <- toolIlMerge) (TimeSpan.FromMinutes 5.0)
    if result <> 0 then failwithf "ILMerge of EventFlow returned with a non-zero exit code"
    NuGet (fun p -> 
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "Newtonsoft.Json",  GetPackageVersion "./packages/" "Newtonsoft.Json"]
            Publish = false })
            "Source/EventFlow/EventFlow.nuspec"
    )

Target "CreatePackageEventFlowAutofac" (fun _ ->
    let binDir = "Source/EventFlow.Autofac/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.Autofac.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.Autofac"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "Autofac",  GetPackageVersion "./packages/" "Autofac"]
            Publish = false })
            "Source/EventFlow.Autofac/EventFlow.Autofac.nuspec"
    )

Target "CreatePackageEventFlowMsSql" (fun _ ->
    let binDir = "Source\\EventFlow.MsSql\\bin\\" + buildMode + "\\"
    let result = ExecProcess (fun info ->
       info.Arguments <- "/targetplatform:v4 /internalize /allowDup /target:library /out:Source\\EventFlow.MsSql\\bin\\EventFlow.MsSql.dll " + binDir + "EventFlow.MsSql.dll " + binDir + "Dapper.dll "  + binDir + "DbUp.dll"
       info.FileName <- toolIlMerge) (TimeSpan.FromMinutes 5.0)
    if result <> 0 then failwithf "ILMerge of EventFlow.MsSql returned with a non-zero exit code"
    NuGet (fun p -> 
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.MsSql"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep]
            Publish = false })
            "Source/EventFlow.MsSql/EventFlow.MsSql.nuspec"
    )

Target "CreatePackageEventFlowEventStoresMsSql" (fun _ ->
    let binDir = "Source/EventFlow.EventStores.MsSql/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.EventStores.MsSql.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.EventStores.MsSql"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "EventFlow.MsSql",  nugetVersionDep]
            Publish = false })
            "Source/EventFlow.EventStores.MsSql/EventFlow.EventStores.MsSql.nuspec"
    )

Target "CreatePackageEventFlowEventStoresEventStore" (fun _ ->
    let binDir = "Source/EventFlow.EventStores.EventStore/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.EventStores.EventStore.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.EventStores.EventStore"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "EventStore.Client",  GetPackageVersion "./packages/" "EventStore.Client"]
            Publish = false })
            "Source/EventFlow.EventStores.EventStore/EventFlow.EventStores.EventStore.nuspec"
    )

Target "CreatePackageEventFlowReadStoresMsSql" (fun _ ->
    let binDir = "Source/EventFlow.ReadStores.MsSql/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.ReadStores.MsSql.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.ReadStores.MsSql"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "EventFlow.MsSql",  nugetVersionDep]
            Publish = false })
            "Source/EventFlow.ReadStores.MsSql/EventFlow.ReadStores.MsSql.nuspec"
    )

Target "CreatePackageEventFlowOwin" (fun _ ->
    let binDir = "Source/EventFlow.Owin/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.Owin.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.Owin"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "Owin",  GetPackageVersion "./packages/" "Owin"
                "Microsoft.Owin",  GetPackageVersion "./packages/" "Microsoft.Owin"]
            Publish = false })
            "Source/EventFlow.Owin/EventFlow.Owin.nuspec"
    )

Target "Default" DoNothing

"Clean"
    ==> "SetVersion"
    ==> "BuildApp"
    ==> "UnitTest"
    ==> "CreatePackageEventFlow"
    ==> "CreatePackageEventFlowAutofac"
    ==> "CreatePackageEventFlowMsSql"
    ==> "CreatePackageEventFlowEventStoresMsSql"
    ==> "CreatePackageEventFlowReadStoresMsSql"
    ==> "CreatePackageEventFlowEventStoresEventStore"
    ==> "CreatePackageEventFlowOwin"
    ==> "Default"

RunTargetOrDefault "Default"
