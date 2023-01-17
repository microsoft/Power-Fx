
$localCache = $Env:USERPROFILE + "\.nuget\packages"

Write-Host "Deleting Microsoft.PowerFx.*\0.3.0-local from nuget cache. This will cause the newly built packages to be downloaded."

Get-ChildItem $localCache Microsoft.PowerFx.* | ForEach-Object { 
    $path = ($_.FullName + "\0.3.0-local")

    if (Test-Path $path) {
      Remove-Item $path -Recurse 
    }
}