@echo off
setlocal enabledelayedexpansion

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

set TESTFILES=%TESTFILE1% %TESTFILE2% %TESTFILE3% %TESTFILE4% %TESTFILE5% %TESTFILE6% %TESTFILE7% %TESTFILE8% %TESTFILE9% %TESTFILE10% %TESTFILE11%

"%VSTEST%" %TESTFILES% /settings:local.runsettings /EnableCodeCoverage /InIsolation
