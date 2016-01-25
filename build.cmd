rem @echo off

".paket\paket.bootstrapper.exe"
IF %errorlevel% NEQ 0 (
	exit /b %errorlevel%
)

".paket\paket.exe" restore
IF %errorlevel% NEQ 0 (
	exit /b %errorlevel%
)

"packages\build\FAKE\tools\Fake.exe" "build.fsx" "nugetApikey=%NUGET_APIKEY%" "buildVersion=%APPVEYOR_BUILD_VERSION%"

exit /b %errorlevel%
