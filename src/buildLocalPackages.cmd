@echo off
setlocal enabledelayedexpansion

set MSBUILDARGS=-p:PublishRepositoryUrl=true -p:GeneratePackages=true -p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg -p:InternalBuild=true -p:Configuration=Debug -p:Platform="Any CPU"
set MSBUILD=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\amd64\MSBuild.exe

@REM Restore dependencies first if needed
"%MSBUILD%" -t:restore

@REM Run build and generate nuget packages 
"%MSBUILD%" %MSBUILDARGS%
