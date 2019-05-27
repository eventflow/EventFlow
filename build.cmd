set CAKE_SETTINGS_SKIPPACKAGEVERSIONCHECK=true
powershell -NoProfile -ExecutionPolicy unrestricted -Command .\build.ps1 --bootstrap
powershell -NoProfile -ExecutionPolicy unrestricted -Command .\build.ps1 -Target %1
