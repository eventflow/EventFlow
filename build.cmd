rem @echo off

mkdir Tools
powershell -Command "if ((Test-Path '.\Tools\NuGet.exe') -eq $false) {(New-Object System.Net.WebClient).DownloadFile('http://nuget.org/nuget.exe', '.\Tools\NuGet.exe')}"

".\Tools\NuGet.exe" "install" "FAKE.Core" "-OutputDirectory" "Tools" "-ExcludeVersion" "-version" "4.9.3"
".\Tools\NuGet.exe" "install" "NUnit.Runners" "-OutputDirectory" "Tools" "-ExcludeVersion" "-version" "2.6.4"
".\Tools\NuGet.exe" "install" "ilmerge" "-OutputDirectory" "Tools" "-ExcludeVersion" "-version" "2.14.1208"

".\Tools\NuGet.exe" restore .\EventFlow.sln

"Tools\FAKE.Core\tools\Fake.exe" "build.fsx" "nugetApikey=%NUGET_APIKEY%" "buildVersion=%APPVEYOR_BUILD_VERSION%"

exit /b %errorlevel%
