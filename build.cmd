rem @echo off

PowerShell -NoProfile -ExecutionPolicy Bypass -Command "& '.\build.ps1' -Target %1 -ScriptArgs '-nugetApikey=%NUGET_APIKEY% -buildVersion=%APPVEYOR_BUILD_VERSION%'";

exit /b %errorlevel%