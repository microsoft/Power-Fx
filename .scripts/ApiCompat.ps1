
param(
	[Parameter(Mandatory = $true)] [String]$BuildConfiguration
)

Write-Host "BuildConfiguration" $BuildConfiguration

$apiTool="tool\.store\microsoft.dotnet.apicompat.tool\7.0.100\microsoft.dotnet.apicompat.tool\7.0.100\tools\net6.0\any\Microsoft.DotNet.ApiCompat.Tool.dll"        
$apiToolRightFolder="Output"

[xml]$xfiles = Get-Content .\ApiCompat.Files.xml
$e = "<br />" + [char]0x274C

foreach ($a in $xfiles.ApiCompat.Assembly)
{
    $n = $a.Name
    $s = "suppress.$n.xml"
    $l = $ExecutionContext.InvokeCommand.ExpandString($a.Location)
    $apiToolLeft = "$l\$n.dll"
    $apiToolRight = "$apiToolRightFolder\$n.dll"
    
    Set-Content $s "<Suppressions/>"

    Write-Host "Checking" $n "..."
    dotnet $apiTool --suppression-file $s --left-assembly $apiToolLeft --right-assembly $apiToolRight --enable-rule-attributes-must-match --enable-rule-cannot-change-parameter-name --generate-suppression-file
    
    [xml]$x = Get-Content $s
    $e += [string]::Join("<br />" + [char]0x274C, ($x.Suppressions.Suppression | % { $_.DiagnosticId + " " + $_.Target })).Trim()
}


if ($e.Length -gt 8)
{
    Write-Host "##[error] $e"
    Write-Host "##vso[task.setvariable variable=ApiCompat;issecret=false]Public API changes: $e"
}
else
{
    $ok = [char]0x2705
    $okMsg = $ok + " No public API change."
    Write-Host $okMsg
    Write-Host "##vso[task.setvariable variable=ApiCompat;issecret=false]$okMsg"
}
