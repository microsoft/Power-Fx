@echo off
setlocal

if not exist "BenchmarkDotNet.Artifacts" md "BenchmarkDotNet.Artifacts"
wmic cpu get Caption, CurrentClockSpeed, Name, NumberOfCores, NumberOfLogicalProcessors > BenchmarkDotNet.Artifacts\cpu.csv
wmic computersystem get TotalPhysicalMemory > BenchmarkDotNet.Artifacts\memory.csv

set "benchmarkProject=tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\Microsoft.PowerFx.Performance.Tests.csproj"
set "benchmarkExecutable=tests\.Net7.0\Microsoft.PowerFx.Performance.Tests\bin\Release\net7.0\Microsoft.PowerFx.Performance.Tests.exe"

dotnet build "%benchmarkProject%" -c Release
if errorlevel 1 exit /b %errorlevel%

"%benchmarkExecutable%" --list tree
if errorlevel 1 exit /b %errorlevel%

if "%~1"=="" (
    "%benchmarkExecutable%" --filter *
) else (
    "%benchmarkExecutable%" %*
)

exit /b %errorlevel%
