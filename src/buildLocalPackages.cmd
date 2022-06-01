@echo off
setlocal enabledelayedexpansion

set MSBUILDARGS=-p:PublishRepositoryUrl=true -p:GeneratePackages=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:InternalBuild=true -p:Configuration=Debug -p:Platform="Any CPU"

for /f "usebackq tokens=*" %%i in (`"%programfiles(x86)%\Microsoft Visual Studio\Installer\vswhere" -latest -requires Microsoft.Component.MSBuild -find MSBuild\**\Bin\MSBuild.exe`) do (
    @REM Restore dependencies first if needed
    "%%i" -t:restore
    @REM Run build and generate nuget packages 
    "%%i" %MSBUILDARGS%
    exit /b !errorlevel!
)