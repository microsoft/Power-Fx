pwsh.exe -executionpolicy bypass -file %~dp0refreshLocalNugetCache.ps1

call buildLocalPackages.cmd

@dir %~dp0outputpackages

@echo To consume these nugets locally:
@echo 1) Add this to your nuget.config:
@echo  ^<add key="Local" value="%~dp0outputpackages" /^>
@echo .  
@echo 2) Set your current PowerFx version to 
@echo  0.3.0-local
