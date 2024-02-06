@echo off
setlocal

rem install ajv with "npm install -g ajv-cli"
rem source at https://github.com/ajv-validator/ajv-cli and https://github.com/ajv-validator/ajv
rem filename parsing drives the tests, in the format "schema-result-....yaml"

rem use "json" as first parameter to use json version of schema files otherwise yaml files are used, for example "run json"
rem use "verbose" to show errors for invalid tests, for examle "run verbose"
rem use first parameter to run just one test, for example "run book.valid.type.fx.yaml"

set enc=yaml
if "%1" == "json" (
    set enc=json
    shift
    echo === Using JSON schema files
)

set command=test --errors=no
if "%1" == "verbose" (
    set command=test
    shift
    echo === Showing errors for invalid tests
)

if not "%1" == "" (
    call :each %1
    goto :EOF
)

set total=0
set fail=0
for %%f in (*.yaml) do call :each %%f
echo %fail% failed, %total% total
goto :EOF

:each
for /f "tokens=1,2,3,* delims=." %%a in ("%1") do call :parse %%a %%b %%c %%d

set rfunction=
set rtype=
if not "%type%"=="function" set rfunction=-r ..\function.fx.schema.%enc%
if not "%type%"=="type" set rtype=-r ..\type.fx.schema.%enc%

cmd /c "ajv %command% -s ..\%type%.fx.schema.%enc% -d %1 --spec=draft7 --strict-types=false %result% %errors% %rtype% %rfunction% 
if errorlevel 1 set /a fail=fail+1
set /a total=total+1
goto :EOF

:parse
set type=%3
if not "%command%"=="" set result=--%2
set test=%1
