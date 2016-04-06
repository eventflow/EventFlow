#r @"packages\build\FAKE\tools\FakeLib.dll"
open System
open Fake 
open Fake.AssemblyInfoFile
open Fake.OpenCoverHelper

let releaseNotes = 
    ReadFile "RELEASE_NOTES.md"
    |> ReleaseNotesHelper.parseReleaseNotes

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let buildVersion = getBuildParamOrDefault "buildVersion" "0.0.1"
let nugetApikey = getBuildParamOrDefault "nugetApikey" ""

let dirPackages = "./Build/Packages"
let dirReports = "./Build/Reports"
let filePathUnitTestReport = dirReports + "/NUnit.xml"
let toolIlMerge = "./packages/build/ilmerge/tools/ILMerge.exe"
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
    let assembliesToTest = (" ", (!! ("**/bin/" @@ buildMode @@ "/EventFlow*Tests.dll"))) |> System.String.Join
    OpenCover
        (fun p -> { 
            p with 
                ExePath = "./packages/test/OpenCover/tools/OpenCover.Console.exe"
                TestRunnerExePath = "./packages/build/NUnit.Runners/tools/nunit-console.exe"
                Output = dirReports + "/opencover-results-unit.xml"
                TimeOut = TimeSpan.FromMinutes 30.0;
                Register = RegisterUser
        })
        ("/nologo /include:unit /noshadow /framework=net-4.5.1 /result=" + dirReports + "/nunit-results-unit.xml " + assembliesToTest)
    )

Target "IntegrationTest" (fun _ ->
    let assembliesToTest = (" ", (!! ("**/bin/" @@ buildMode @@ "/EventFlow*Tests.dll"))) |> System.String.Join
    OpenCover
        (fun p -> { 
            p with 
                ExePath = "./packages/test/OpenCover/tools/OpenCover.Console.exe"
                TestRunnerExePath = "./packages/build/NUnit.Runners/tools/nunit-console.exe"
                Output = dirReports + "/opencover-results-integration.xml"
                TimeOut = TimeSpan.FromMinutes 30.0;
                Register = RegisterUser
        })
        ("/nologo /include:integration /noshadow /framework=net-4.5.1 /result=" + dirReports + "/nunit-results-integration.xml " + assembliesToTest)
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

Target "CreatePackageEventFlowRabbitMQ" (fun _ ->
    let binDir = "Source/EventFlow.RabbitMQ/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.RabbitMQ.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.RabbitMQ"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "RabbitMQ.Client",  GetPackageVersion "./packages/" "RabbitMQ.Client"]
            Publish = false })
            "Source/EventFlow.RabbitMQ/EventFlow.RabbitMQ.nuspec"
    )

Target "CreatePackageEventFlowHangfire" (fun _ ->
    let binDir = "Source/EventFlow.Hangfire/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.Hangfire.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.Hangfire"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "Hangfire.Core",  "1.5.3"]
            Publish = false })
            "Source/EventFlow.Hangfire/EventFlow.Hangfire.nuspec"
    )

Target "CreatePackageEventFlowSql" (fun _ ->
    let binDir = "Source\\EventFlow.Sql\\bin\\" + buildMode + "\\"
    let result = ExecProcess (fun info ->
       info.Arguments <- "/targetplatform:v4 /internalize /allowDup /target:library /out:Source\\EventFlow.Sql\\bin\\EventFlow.Sql.dll " + binDir + "EventFlow.Sql.dll " + binDir + "dbup.dll"
       info.FileName <- toolIlMerge) (TimeSpan.FromMinutes 5.0)
    NuGet (fun p -> 
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.Sql"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "Dapper", GetPackageVersion "./packages/" "Dapper"]
            Publish = false })
            "Source/EventFlow.Sql/EventFlow.Sql.nuspec"
    )

Target "CreatePackageEventFlowMsSql" (fun _ ->
    let binDir = "Source/EventFlow.MsSql/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.MsSql.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.MsSql"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "EventFlow.Sql",  nugetVersionDep
                "Dapper", GetPackageVersion "./packages/" "Dapper"]
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

Target "CreatePackageEventFlowSQLite" (fun _ ->
    let binDir = "Source/EventFlow.SQLite/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.SQLite.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.SQLite"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "EventFlow.Sql",  nugetVersionDep
                "System.Data.SQLite.Core",  GetPackageVersion "./packages/" "System.Data.SQLite.Core"]
            Publish = false })
            "Source/EventFlow.SQLite/EventFlow.SQLite.nuspec"
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

Target "CreatePackageEventFlowReadStoresElasticsearch" (fun _ ->
    let binDir = "Source/EventFlow.ReadStores.Elasticsearch/bin/"
    CopyFile binDir (binDir + buildMode + "/EventFlow.ReadStores.Elasticsearch.dll")
    NuGet (fun p ->
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow.ReadStores.Elasticsearch"
            Version = nugetVersion
            ReleaseNotes = toLines releaseNotes.Notes
            Dependencies = [
                "EventFlow",  nugetVersionDep
                "NEST",  GetPackageVersion "./packages/" "NEST"
                "Elasticsearch.Net",  GetPackageVersion "./packages/" "Elasticsearch.Net"
                "Elasticsearch.Net.JsonNET",  GetPackageVersion "./packages/" "Elasticsearch.Net.JsonNET"]
            Publish = false })
            "Source/EventFlow.ReadStores.Elasticsearch/EventFlow.ReadStores.Elasticsearch.nuspec"
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
    ==> "IntegrationTest"
    ==> "CreatePackageEventFlow"
    ==> "CreatePackageEventFlowAutofac"
    ==> "CreatePackageEventFlowRabbitMQ"
    ==> "CreatePackageEventFlowHangfire"
    ==> "CreatePackageEventFlowSql"
    ==> "CreatePackageEventFlowMsSql"
    ==> "CreatePackageEventFlowSQLite"
    ==> "CreatePackageEventFlowEventStoresMsSql"
    ==> "CreatePackageEventFlowReadStoresMsSql"
    ==> "CreatePackageEventFlowReadStoresElasticsearch"
    ==> "CreatePackageEventFlowEventStoresEventStore"
    ==> "CreatePackageEventFlowOwin"
    ==> "Default"

RunTargetOrDefault "Default"
