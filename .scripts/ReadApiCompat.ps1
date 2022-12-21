[string]::Join(", ", (([xml](Get-Content .\suppress.xml)).Suppressions.Suppression | % { $_.DiagnosticId + ": " + $_.Target })) > supp.txt
