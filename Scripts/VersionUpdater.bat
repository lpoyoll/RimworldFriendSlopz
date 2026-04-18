@echo off

::Set variables
set "DashLine=----------"

::Set custom title
title RimWorld Together - Version Updater

::Set rimworld folder path
set GameFolder=%CD%

::Wait for RimWorld safe close
echo %DashLine%
echo - Waiting for RimWorld to safely close...
echo %DashLine%
timeout /t 5

::Go to temp folder
cd %LOCALAPPDATA%\..\LocalLow
cd "Ludeon Studios"
cd "RimWorld by Ludeon Studios"
cd "RimWorld Together"
cd "Temp"

::Set mod folder path
set /p ModFolder=<ModPath.txt

::Go to mod folder
cd %ModFolder%\..

::Clean old folder
rmdir /s /q "3005289691"

::Replace with new installation
echo.
echo %DashLine%
echo - Installing new version
move "3005289691-Temp" "3005289691"
echo %DashLine%

::Wait at end
echo.
echo %DashLine%
echo - Operation finished...
echo.
echo - Game will open again soon...
echo.
echo - Please press any key or wait for the window to close...
echo %DashLine%
timeout /t 10

::Open game
cd %GameFolder%
start RimWorldWin64.exe