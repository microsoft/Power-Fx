@echo off
setlocal

rem install ajv with "npm install -g ajv-cli"
rem source at https://github.com/ajv-validator/ajv-cli and https://github.com/ajv-validator/ajv
rem filename parsing drives the tests, in the format "schema-result-....yaml"

for %%f in (*.schema.yaml) do call :proc %%f %%~nf.json
goto :EOF

:proc
rem echo %1 %2
cmd /c "ajv migrate -s %1 -o %2"
pwsh -Command "(Get-Content %2) -replace '.schema.yaml', '.schema.json' | out-file %2"
