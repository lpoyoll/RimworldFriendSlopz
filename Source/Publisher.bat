@echo off

SET RUNTIME="NULL"
SET MAINFOLDER=%CD%
SET OUTPUTDIR=%MAINFOLDER%/%RUNTIME%

rmdir "Publish" /s /q >nul 2>&1
cd Server

cls
echo - Exporting projects
echo.

SET RUNTIME="win-x86"
SET OUTPUTDIR="%MAINFOLDER%/Publish/%RUNTIME%"
dotnet publish "GameServer.csproj" --configuration "Release" --runtime %RUNTIME% --self-contained true --output %OUTPUTDIR% -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
echo.

SET RUNTIME="win-x64"
SET OUTPUTDIR="%MAINFOLDER%/Publish/%RUNTIME%"
dotnet publish "GameServer.csproj" --configuration "Release" --runtime %RUNTIME% --self-contained true --output %OUTPUTDIR% -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
echo.

SET RUNTIME="linux-x64"
SET OUTPUTDIR="%MAINFOLDER%/Publish/%RUNTIME%"
dotnet publish "GameServer.csproj" --configuration "Release" --runtime %RUNTIME% --self-contained true --output %OUTPUTDIR% -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
echo.

SET RUNTIME="linux-arm"
SET OUTPUTDIR="%MAINFOLDER%/Publish/%RUNTIME%"
dotnet publish "GameServer.csproj" --configuration "Release" --runtime %RUNTIME% --self-contained true --output %OUTPUTDIR% -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
echo.

SET RUNTIME="linux-arm64"
SET OUTPUTDIR="%MAINFOLDER%/Publish/%RUNTIME%"
dotnet publish "GameServer.csproj" --configuration "Release" --runtime %RUNTIME% --self-contained true --output %OUTPUTDIR% -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false
echo.

cls
echo - Compressing exported files
echo.

cd %MAINFOLDER%
cd Publish

SET RUNTIME="win-x86"
"C:\Program Files\7-Zip\7z.exe" a %RUNTIME%.zip ./%RUNTIME%/*
rmdir %RUNTIME% /s /q

SET RUNTIME="win-x64"
"C:\Program Files\7-Zip\7z.exe" a %RUNTIME%.zip ./%RUNTIME%/*
rmdir %RUNTIME% /s /q

SET RUNTIME="linux-x64"
"C:\Program Files\7-Zip\7z.exe" a %RUNTIME%.zip ./%RUNTIME%/*
rmdir %RUNTIME% /s /q

SET RUNTIME="linux-arm"
"C:\Program Files\7-Zip\7z.exe" a %RUNTIME%.zip ./%RUNTIME%/*
rmdir %RUNTIME% /s /q

SET RUNTIME="linux-arm64"
"C:\Program Files\7-Zip\7z.exe" a %RUNTIME%.zip ./%RUNTIME%/*
rmdir %RUNTIME% /s /q

cls
echo - Process finished
pause