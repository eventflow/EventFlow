#r @"tools\FAKE.Core\tools\FakeLib.dll"
open Fake 
open System

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let buildVersion = getBuildParamOrDefault "buildVersion" "0.0.1"
let nugetApikey = getBuildParamOrDefault "nugetApikey" ""

let dirPackages = "./Build/Packages"
let dirReports = "./Build/Reports"
let filePathUnitTestReport = dirReports + "/NUnit.xml"
let fileListUnitTests = !! ("**/bin/" @@ buildMode @@ "/EventFlow*Tests.dll")
let toolNUnit = "./Tools/NUnit.Runners/tools"
let toolIlMerge = "./Tools/ilmerge/tools/ILMerge.exe"


Target "Clean" (fun _ ->
    CleanDirs [ dirPackages; dirReports ]
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
    let eventFlowBinDir = "Source\\EventFlow\\bin\\" + buildMode + "\\"

    let result = ExecProcess (fun info ->
       info.Arguments <- "/targetplatform:v4 /internalize /allowDup /target:library /out:Source\\EventFlow\\bin\\EventFlow.dll " + eventFlowBinDir + "EventFlow.dll " + eventFlowBinDir + "Autofac.dll"
       info.FileName <- toolIlMerge) (TimeSpan.FromMinutes 5.0)
     
    if result <> 0 then failwithf "ILMerge returned with a non-zero exit code"

    NuGet (fun p -> 
        {p with
            OutputPath = dirPackages
            WorkingDir = "Source/EventFlow"
            Version = buildVersion + "-alpha"
            Dependencies = [
                "Common.Logging", GetPackageVersion "./packages/" "Common.Logging"
                "Common.Logging.Core",  GetPackageVersion "./packages/" "Common.Logging.Core"
                "CommonServiceLocator",  GetPackageVersion "./packages/" "CommonServiceLocator"
                "Newtonsoft.Json",  GetPackageVersion "./packages/" "Newtonsoft.Json"]
            Publish = false })
            "Source/EventFlow/EventFlow.nuspec"
    )

Target "Default" DoNothing

"Clean"
    ==> "BuildApp"
    ==> "UnitTest"
    ==> "CreatePackageEventFlow"
    ==> "Default"

RunTargetOrDefault "Default"

