if (-not (Test-Path "BenchmarkDotNet.Artifacts"))
{
    Write-Host -ForegroundColor Yellow "No BenchmarkDotNet.Artifacts folder!"
    Write-Host -ForegroundColor DarkYellow "Run benchmark first."
    exit
}

function ConvertToMs([string]$str)
{
    try
    {
        if (($str.Trim() -eq '-') -or [string]::IsNullOrEmpty($str.Trim()))
        {
            $val = [double]0
        }
        else
        {
            $parts = $str.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
            $val = [double]$parts[0]

            ## Convert to milliseconds
            if     ($parts[1] -eq "s")  { $val *= 1000    }
            elseif ($parts[1] -eq "ms") {                 } ## Do nothing
            elseif ($parts[1] -eq "µs") { $val /= 1000    }
            elseif ($parts[1] -eq "ns") { $val /= 1000000 }
            else   { throw ("Unknown unit: " + $parts[1]) }
        }

        $val
    }
    catch
    {
        Write-Error $_               
    }
}

function ConvertToBytes([string]$str)
{
    try
    {
        $parts = $str.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
        $val = [double]$parts[0]

        ## Convert to milliseconds
        if     ($parts[1] -eq "B")  {                    } ## Do nothing
        elseif ($parts[1] -eq "kB") { $val *= 1024       } 
        elseif ($parts[1] -eq "MB") { $val *= 1048576    }
        elseif ($parts[1] -eq "GB") { $val *= 1073741824 }
        else   { throw ("Unknown unit: " + $parts[1]) }

        $val
    }
    catch
    {
        Write-Error $_               
    }
}

Write-Host "------ Test context ------"

$cpuFile = "BenchmarkDotNet.Artifacts\cpu.csv"
$memoryFile = "BenchmarkDotNet.Artifacts\memory.csv"
$referenceFile = "BenchmarkDotNet.Artifacts\results\Microsoft.PowerFx.Performance.Tests.Reference-report-github.md"

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

    if ($index -eq 1)
    {
        $memory = [double]::Parse($line.Substring($hdrs[0], $hdrs[1] - $hdrs[0]).Trim());
    }

    $index++
}

$memoryGB = $memory / [double]1024 / [double]1024 / [double]1024;

Write-Host "Memory (GB)       : $memoryGB"

$index = 0
foreach ($line in (Get-Content $referenceFile))
{
    if ($index -in @(2, 4))
    {
        foreach ($a in ($line.Split(@(','), [System.StringSplitOptions]::RemoveEmptyEntries) | % { $_.Trim() }))
        {
            $b = $a.Split(@('='), [System.StringSplitOptions]::RemoveEmptyEntries) | % { $_.Trim() }
            if ($b[0] -eq 'BenchmarkDotNet') { $bmdnVersion = $b[1] }
            if ($b[0] -eq 'OS') { $osVersion = $b[1] }
            if ($b[0] -eq 'VM') { $vmType = $b[1] }
            if ($b[0] -in @('.Net Core SDK', '.NET SDK')) { $dnRTVersion = $b[1] }
        }        
    }    
    if ($index -eq 6)
    {
        $a = $line.Split(@(':'), [System.StringSplitOptions]::RemoveEmptyEntries) | % { $_.Trim() }
        $dnVersion = $a[1]
    }

    $index++
}

Write-Host "BenchmarkDotNet   : $bmdnVersion"
Write-Host "OS Version        : $osVersion"
Write-Host "VM Type           : $vmType"
Write-Host ".Net RT Version   : $dnRTVersion"
Write-Host ".Net Version      : $dnVersion"

Write-Host

## Always start the list of results with the Reference CSV file
$list = (Get-Item -Filter *.csv -Path '.\BenchmarkDotNet.Artifacts\results\*' | % { $_.FullName })
foreach ($file in [System.Linq.Enumerable]::OrderBy($list, [Func[object, string]] { param($s) if ($s -match 'Reference-report\.csv') { "" } else { $s } }))
{
    $t = [System.IO.Path]::GetFileNameWithoutExtension($file).Split(@('.'))[-1]
    $testCategory = $t.Substring(0, $t.Length - 7)

    Write-Host "------ [TEST] $testCategory ------"
   
    $table = [System.Data.DataTable]::new()
    [void]$table.Columns.Add("TestName", [string]);                $table.Columns["TestName"].AllowDBNull = $false    
    [void]$table.Columns.Add("N", [int]);                          $table.Columns["N"].AllowDBNull = $true                 ## Optional column, depends on the test
    [void]$table.Columns.Add("Mean", [double]);                    $table.Columns["Mean"].AllowDBNull = $false
    [void]$table.Columns.Add("StdDev", [double]);                  $table.Columns["StdDev"].AllowDBNull = $false
    [void]$table.Columns.Add("Min", [double]);                     $table.Columns["Min"].AllowDBNull = $false
    [void]$table.Columns.Add("Q1", [double]);                      $table.Columns["Q1"].AllowDBNull = $false
    [void]$table.Columns.Add("Median", [double]);                  $table.Columns["Median"].AllowDBNull = $false
    [void]$table.Columns.Add("Q3", [double]);                      $table.Columns["Q3"].AllowDBNull = $false
    [void]$table.Columns.Add("Max", [double]);                     $table.Columns["Max"].AllowDBNull = $false
    [void]$table.Columns.Add("Gen0", [double]);                    $table.Columns["Gen0"].AllowDBNull = $false
    [void]$table.Columns.Add("Gen1", [double]);                    $table.Columns["Gen1"].AllowDBNull = $false
    [void]$table.Columns.Add("Allocated", [double]);               $table.Columns["Allocated"].AllowDBNull = $false
    [void]$table.Columns.Add("AllocatedNativeMemory", [double]);   $table.Columns["AllocatedNativeMemory"].AllowDBNull = $false
    [void]$table.Columns.Add("NativeMemoryLeak", [double]);        $table.Columns["NativeMemoryLeak"].AllowDBNull = $false

    foreach ($row in (Import-Csv $file | Select-Object Method, Runtime, N, Mean, StdDev, Min, Q1, Median, Q3, Max, Gen0, Gen1, Allocated, 'Allocated Native Memory', 'Native Memory Leak'))
    {
        $mean = ConvertToMs($row.Mean)
        $stddev = ConvertToMs($row.StdDev)
        $min = ConvertToMs($row.Min)
        $q1 = ConvertToMs($row.Q1)
        $median = ConvertToMs($row.Median)
        $q3 = ConvertToMs($row.Q3)
        $max = ConvertToMs($row.Max)
        $gen0 = $row.Gen0                           ## Gen X means number of GC collections per 1000 operations for that generation
        $gen1 = $row.Gen1
        $alloc = ConvertToBytes($row.Allocated)
        $native = ConvertToBytes($row.'Allocated Native Memory')
        $leak = ConvertToBytes($row.'Native Memory Leak')

        if (($gen0 -eq $null) -or ($gen0.Trim() -eq '-') -or  [string]::IsNullOrEmpty($gen0.Trim())) { $gen0 = [double]0 }
        if (($gen1 -eq $null) -or ($gen1.Trim() -eq '-') -or  [string]::IsNullOrEmpty($gen1.Trim())) { $gen1 = [double]0 }

        [void]$table.Rows.Add($row.Method, $row.N, $mean, $stddev, $min, $q1, $median, $q3, $max, $gen0, $gen1, $alloc, $native, $leak)
    }

    $table | Sort-Object TestName, N | ft TestName, N, Mean, StdDev, Min, Q1, Median, Q3, Max, Gen0, Gen1, Allocated, AllocatedNativeMemory, NativeMemoryLeak
    Write-Host
}

Write-Host '--- End of script ---'
