@echo off
setlocal enabledelayedexpansion

set CONFIGURATION=DebugAll
if /i _%1 neq _ @set CONFIGURATION=%1
@echo Building %Configuration%

set MSBUILDARGS=-p:PublishRepositoryUrl=true -p:GeneratePackages=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:InternalBuild=true -p:Configuration=%CONFIGURATION% -p:Platform="Any CPU" -verbosity:m
set MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe

@REM Restore dependencies first if needed
"%MSBUILD%" -t:restore -p:Configuration=DebugAll -verbosity:m

@REM Run build and generate nuget packages 
"%MSBUILD%" %MSBUILDARGS%
