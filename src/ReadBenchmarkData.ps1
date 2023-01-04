if (-not (Test-Path "BenchmarkDotNet.Artifacts"))
{
    Write-Host -ForegroundColor Yellow "No BenchmarkDotNet.Artifacts folder!"
    Write-Host -ForegroundColor DarkYellow "Run benchmark first."
    exit
}

$cpuFile = "BenchmarkDotNet.Artifacts\cpu.csv"
$memoryFile = "BenchmarkDotNet.Artifacts\memory.csv"

## Read CPU file, fixed size columns
$index = 0; $x = 0
foreach ($line in (Get-Content $cpuFile))
{
    ## Get headers and identify their positon
    if ($index -eq 0)
    {
        $headers = $line.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
        $hdrs = [System.Collections.ArrayList]::new()
        $l = $line
        for ($i = 0; $i -lt $headers.Length; $i++)
        {           
            $y = $line.IndexOf($headers[$i])              
            $x += $y
            [void]$hdrs.Add($x)
            $line = $line.Substring($headers[$i].Length + $y )
            $x += $headers[$i].Length
        }

        [void]$hdrs.Add($x)
    }

    if ($index -eq 1)
    {
        $cpuModel           = $line.Substring($hdrs[0], $hdrs[1] - $hdrs[0]).Trim()
        $cpuSpeed           = [int]::Parse($line.Substring($hdrs[1], $hdrs[2] - $hdrs[1]).Trim())
        $cpuName            = $line.Substring($hdrs[2], $hdrs[3] - $hdrs[2]).Trim()
        $numberCores        = [int]::Parse($line.Substring($hdrs[3], $hdrs[4] - $hdrs[3]).Trim())
        $numberLogicalProcs = [int]::Parse($line.Substring($hdrs[4], $hdrs[5] - $hdrs[4]).Trim())
    }

    $index++
}

Write-Host "CPU Model         : $cpuModel"
Write-Host "CPU Speed (MHz)   : $cpuSpeed"
Write-Host "CPU Name          : $cpuName"
Write-Host "Number Cores      : $numberCores"
Write-Host "Logical processors: $numberLogicalProcs"

## Read CPU file, fixed size columns
$index = 0; $x = 0; $memory = [double]0;
foreach ($line in (Get-Content $memoryFile))
{
    ## Get headers and identify their positon
    if ($index -eq 0)
    {
        $headers = $line.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
        $hdrs = [System.Collections.ArrayList]::new()
        $l = $line
        for ($i = 0; $i -lt $headers.Length; $i++)
        {           
            $y = $line.IndexOf($headers[$i])              
            $x += $y
            [void]$hdrs.Add($x)
            $line = $line.Substring($headers[$i].Length + $y )
            $x += $headers[$i].Length
        }

        [void]$hdrs.Add($x)
    }

    if ($index -ne 0)
    {
        $memory += [double]::Parse($line.Substring($hdrs[0], $hdrs[1] - $hdrs[0]).Trim());
    }

    $index++
}

$memoryGB = $memory / [double]1024 / [double]1024 / [double]1024;
Write-Host "Memory (GB)       : $memoryGB"
