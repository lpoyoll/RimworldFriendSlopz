@echo off

::Set variables
set "DashLine=----------"

::Set custom title
title RimWorld Together - Version Updater

::Wait for RimWorld safe close
echo %DashLine%
echo - Waiting for RimWorld to safely close...
echo %DashLine%
timeout /t 5

::Go to default folder
cd Mods
cd Rimworld-Together

::Go to temp folder
cd "Temp"

::Unzip the file
echo.
echo %DashLine%
echo - Extracting archive...
echo %DashLine%
powershell -command "Expand-Archive -Path '3005289691.zip' -DestinationPath '3005289691' -Force"

::Save file location
set "ExtractedFolder=%cd%/3005289691"

::Go to mods folder
cd..
cd..
echo %cd%

::Move folder to temp place
move "%ExtractedFolder%" "3005289691-Temp"
timeout /t 3

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
cd..
start RimWorldWin64.exe