@echo off
setlocal enabledelayedexpansion

echo. | time | findstr /v new

set CONFIGURATION=DebugAll
if /i _%1 neq _ @set CONFIGURATION=%1
@echo Testing %Configuration%

set VSTEST=C:\Program Files\Microsoft Visual Studio\2022\Enterprise\Common7\IDE\Extensions\TestPlatform\vstest.console.exe

set TESTFILE1="tests\.Net 4.6.2\Microsoft.PowerFx.Connectors.Tests\bin\%CONFIGURATION%\net462\Microsoft.PowerFx.Connectors.Tests.dll"
set TESTFILE2="tests\.Net 4.6.2\Microsoft.PowerFx.Core.Tests\bin\%CONFIGURATION%\net462\Microsoft.PowerFx.Core.Tests.dll"
set TESTFILE3="tests\.Net 4.6.2\Microsoft.PowerFx.Interpreter.Tests\bin\%CONFIGURATION%\net462\Microsoft.PowerFx.Interpreter.Tests.dll"
set TESTFILE4="tests\.Net 4.6.2\Microsoft.PowerFx.Json.Tests\bin\%CONFIGURATION%\net462\Microsoft.PowerFx.Json.Tests.dll"
set TESTFILE5="tests\.Net 4.6.2\Microsoft.PowerFx.Repl.Tests\bin\%CONFIGURATION%\net462\Microsoft.PowerFx.Repl.Tests.dll"

set TESTFILE6="tests\.Net 7.0\Microsoft.PowerFx.Connectors.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Connectors.Tests.dll"
set TESTFILE7="tests\.Net 7.0\Microsoft.PowerFx.Core.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Core.Tests.dll"
set TESTFILE8="tests\.Net 7.0\Microsoft.PowerFx.Interpreter.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Interpreter.Tests.dll"
set TESTFILE9="tests\.Net 7.0\Microsoft.PowerFx.Json.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Json.Tests.dll"
set TESTFILE10="tests\.Net 7.0\Microsoft.PowerFx.Performance.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Performance.Tests.dll"
set TESTFILE11="tests\.Net 7.0\Microsoft.PowerFx.Repl.Tests\bin\%CONFIGURATION%\net7.0\Microsoft.PowerFx.Repl.Tests.dll"

if exist %TESTFILE1% set TESTFILES462=%TESTFILES462% %TESTFILE1%
if exist %TESTFILE2% set TESTFILES462=%TESTFILES462% %TESTFILE2%
if exist %TESTFILE3% set TESTFILES462=%TESTFILES462% %TESTFILE3%
if exist %TESTFILE4% set TESTFILES462=%TESTFILES462% %TESTFILE4%
if exist %TESTFILE5% set TESTFILES462=%TESTFILES462% %TESTFILE5%

if exist %TESTFILE6% set TESTFILES70=%TESTFILES70% %TESTFILE6%
if exist %TESTFILE7% set TESTFILES70=%TESTFILES70% %TESTFILE7%
if exist %TESTFILE8% set TESTFILES70=%TESTFILES70% %TESTFILE8%
if exist %TESTFILE9% set TESTFILES70=%TESTFILES70% %TESTFILE9%
if exist %TESTFILE10% set TESTFILES70=%TESTFILES70% %TESTFILE10%
if exist %TESTFILE11% set TESTFILES70=%TESTFILES70% %TESTFILE11%

rem /EnableCodeCoverage /InIsolation

@echo Running .Net 4.6.2 tests...
"%VSTEST%" %TESTFILES462% /settings:local.runsettings /logger:trx /Parallel /logger:console;verbosity=quiet /TestCaseFilter:"Net=462"
echo. | time | findstr /v new

@echo Running .Net 7.0 tests...
"%VSTEST%" %TESTFILES70% /settings:local.runsettings /logger:trx /Parallel /logger:console;verbosity=quiet /TestCaseFilter:"Net=70"
echo. | time | findstr /v new
