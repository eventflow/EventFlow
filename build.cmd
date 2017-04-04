rem @echo off

IF EXIST ".paket\paket.exe" GOTO has-packet

".paket\paket.bootstrapper.exe" "4.0.0-beta002"
IF %errorlevel% NEQ 0 (
	exit /b %errorlevel%
)

:has-packet
".paket\paket.exe" restore
IF %errorlevel% NEQ 0 (
	exit /b %errorlevel%
)

"packages\build\Cake\Cake.exe" build.cake -nugetApikey=%NUGET_APIKEY% -buildVersion=%APPVEYOR_BUILD_VERSION% -target=%1

exit /b %errorlevel%