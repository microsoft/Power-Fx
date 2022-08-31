@echo off
setlocal

rem install ajv with "npm install -g ajv-cli"
rem source at https://github.com/ajv-validator/ajv-cli and https://github.com/ajv-validator/ajv
rem filename parsing drives the tests, in the format "schema-result-....yaml"

if not "%1" == "" call :each %1
if not "%1" == "" goto :EOF

set errors=--errors=no

set total=0
set fail=0
for %%f in (*.yaml) do call :each %%f
echo %fail% failed, %total% total
goto :EOF

:each
for /f "tokens=1,2,* delims=-" %%a in ("%1") do call :parse %%a %%b %%c 
set rfunction=
set rtype=
if not "%type%"=="function" set rfunction=-r ..\function.schema.json
if not "%type%"=="type" set rtype=-r ..\type.schema.json
cmd /c "ajv test -s ..\%type%.schema.json -d %1 --spec=draft2019 --%result% %errors% %rtype% %rfunction% --strict=false"
if errorlevel 1 set /a fail=fail+1
set /a total=total+1
goto :EOF

:parse
set type=%1
set result=%2
set test=%3
