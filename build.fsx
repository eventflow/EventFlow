#r @"tools\FAKE.Core\tools\FakeLib.dll"
open Fake 
open System

let buildMode = getBuildParamOrDefault "buildMode" "Release"
let buildVersion = getBuildParamOrDefault "buildVersion" "0.0.1"
let nugetApikey = getBuildParamOrDefault "nugetApikey" ""

let dirPackages = "./Build/Packages"
let dirReports = "./Build/Reports"
let filePathUnitTestReport = dirReports + "/NUnit.xml"
let fileListUnitTests = !! ("**/bin/" @@ buildMode @@ "/EventFlow*.Tests.dll")
let toolNUnit = "./Tools/NUnit.Runners/tools"

Target "Clean" (fun _ -> 
    CleanDirs ["./Build/Deploy/"; dirPackages; dirReports]
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

Target "Default" DoNothing

"Clean"
    ==> "BuildApp"
//    ==> "UnitTest"
    ==> "Default"

RunTargetOrDefault "Default"
