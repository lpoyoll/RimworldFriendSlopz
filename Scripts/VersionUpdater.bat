@echo off

::Set variables
set "DashLine=----------"

::Set custom title
title RimWorld Together - Version Updater

::Set mod folder path
set /p ModFolder=<ModPath.txt
echo "Mods folder located at %ModFolder%"
del ModPath.txt

::Wait for RimWorld safe close
echo %DashLine%
echo - Waiting for RimWorld to safely close...
echo %DashLine%
timeout /t 5

::Replace with new installation
echo.
echo %DashLine%
echo - Installing new version
cd %ModFolder%
rmdir "3005289691"
move "3005289691-Temp" "3005289691-2"
echo %DashLine%

::Wait at end
echo.
echo %DashLine%
echo - Operation finished...
echo - Please press any key or wait for the window to close...
echo %DashLine%
timeout /t 10

::Remove leftover files
del VersionUpdater.bat