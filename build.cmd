rem @echo off

git clean -fxd

"packages\build\Cake\Cake.exe" build.cake -nugetApikey=%NUGET_APIKEY% -buildVersion=%APPVEYOR_BUILD_VERSION% --paths_tools="./packages/build" -target=%1

exit /b %errorlevel%