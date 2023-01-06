param(
	[Parameter (Mandatory = $true)]  [String]$ConnectionString
)

## $env:BUILD_SOURCESDIRECTORY = "C:\Data\2\Power-Fx"
## $env:BUILD_DEFINITIONNAME = "Test"
## $env:BUILD_BUILDID = "69992023"
## $env:BUILD_BUILDNUMBER = "2023.X1"
## $env:BUILDCONFIGURATION = "TestRelease"

function ConvertToMs([string]$str)
{
    try
    {
        $parts = $str.Split(@(' '), [System.StringSplitOptions]::RemoveEmptyEntries)
        $val = [double]$parts[0]

        ## Convert to milliseconds
        if     ($parts[1] -eq "s")  { $val *= 1000    }
        elseif ($parts[1] -eq "ms") {                 } ## Do nothing
        elseif ($parts[1] -eq "µs") { $val /= 1000    }
        elseif ($parts[1] -eq "ns") { $val /= 1000000 }
        else   { throw ("Unknown unit: " + $parts[1]) }

        $val
    }
    catch
    {
        Write-Error $_               
    }
}

## Retrieve Power Fx latest Git hash and Git remote branch
cd ($env:BUILD_SOURCESDIRECTORY)
$pfxHash = (git log -n 1 --pretty=%H).ToString()

$pfxBranch = (git log -n 1 --pretty=%D).Split(", /")
$pfxBranch = $pfxBranch[$pfxBranch.Length-1]

Write-Host "------ Git context ------"
Write-Host "PFX Hash  : $pfxHash"
Write-Host "PFX Branch: $pfxBranch"
Write-Host

cd src

if (-not (Test-Path "BenchmarkDotNet.Artifacts"))
{
    Write-Host -ForegroundColor Yellow "No BenchmarkDotNet.Artifacts folder!"
    Write-Host -ForegroundColor DarkYellow "Run benchmark first."
    dir
    exit
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

$Connection = New-Object System.Data.SQLClient.SQLConnection
$Connection.ConnectionString = $ConnectionString
$Connection.Open()

$Command = New-Object System.Data.SQLClient.SQLCommand
$Command.Connection = $Connection

$insertQuery =  "insert into Runs ([TimeStamp], [Hash], [Pipeline], [BuildId], [BuildNumber], [BuildConfiguration], [Branch]) "
$insertQuery += "values (getutcdate(), '$pfxHash', '$env:BUILD_DEFINITIONNAME', '$env:BUILD_BUILDID', '$env:BUILD_BUILDNUMBER', '$env:BUILDCONFIGURATION', '$pfxBranch');"
$insertQuery += "select scope_identity() as 'Id'"

$Command.CommandText = $insertquery
$runId = $Command.ExecuteScalar()

Write-Host "RunId:     $runId"

$insertQuery =  "insert into Contexts ([RunId], [CPUModel], [CPUSpeedMHz], [CPUName], [NumberCores], [LogicalProcessors], [MemoryGB], [BenchMarkDotNetVersion], [OS], [VM], [DotNetRuntime], [DotNetVersion]) "
$insertQuery += "values ('$runId', '$cpuModel', '$cpuSpeed', '$cpuName', '$numberCores', '$numberLogicalProcs', '$memoryGB', '$bmdnVersion', '$osVersion', '$vmType', '$dnRTVersion', '$dnVersion');"
$insertQuery += "select scope_identity() as 'Id'"

$Command.CommandText = $insertquery
$contextId = $Command.ExecuteScalar()

Write-Host "ContextId: $contextId"
Write-Host

## Always start the list of results with the Reference CSV file
$list = (Get-Item -Filter *.csv -Path '.\BenchmarkDotNet.Artifacts\results\*' | % { $_.FullName })
foreach ($file in [System.Linq.Enumerable]::OrderBy($list, [Func[object, string]] { param($s) if ($s -match 'Reference-report\.csv') { "" } else { $s } }))
{
    $t = [System.IO.Path]::GetFileNameWithoutExtension($file).Split(@('.'))[-1]
    $testCategory = $t.Substring(0, $t.Length - 7)

    Write-Host "------ [TEST] $testCategory ------"
   
    $table = [System.Data.DataTable]::new()
    [void]$table.Columns.Add("TestName", [string]);      $table.Columns["TestName"].AllowDBNull = $false    
    [void]$table.Columns.Add("N", [int]);                $table.Columns["N"].AllowDBNull = $true                 ## Optional column, depends on the test
    [void]$table.Columns.Add("Mean", [double]);          $table.Columns["Mean"].AllowDBNull = $false
    [void]$table.Columns.Add("StdDev", [double]);        $table.Columns["StdDev"].AllowDBNull = $false
    [void]$table.Columns.Add("Min", [double]);           $table.Columns["Min"].AllowDBNull = $false
    [void]$table.Columns.Add("Q1", [double]);            $table.Columns["Q1"].AllowDBNull = $false
    [void]$table.Columns.Add("Median", [double]);        $table.Columns["Median"].AllowDBNull = $false
    [void]$table.Columns.Add("Q3", [double]);            $table.Columns["Q3"].AllowDBNull = $false
    [void]$table.Columns.Add("Max", [double]);           $table.Columns["Max"].AllowDBNull = $false

    foreach ($row in (Import-Csv $file | Select-Object Method, Runtime, N, Mean, StdDev, Min, Q1, Median, Q3, Max))
    {
        $mean = ConvertToMs($row.Mean)
        $stddev = ConvertToMs($row.StdDev)
        $min = ConvertToMs($row.Min)
        $q1 = ConvertToMs($row.Q1)
        $median = ConvertToMs($row.Median)
        $q3 = ConvertToMs($row.Q3)
        $max = ConvertToMs($row.Max)

        [void]$table.Rows.Add($row.Method, $row.N, $mean, $stddev, $min, $q1, $median, $q3, $max)
    }

    $table | Sort-Object TestName, N | ft 

    $ctxids = [System.Collections.ArrayList]::new()
    foreach ($row in $table)
    {
        $testName = $row.TestName
        $n = $row.N
        $mean = $row.Mean
        $stdDev = $row.StdDev
        $min = $row.Min
        $q1 = $row.Q1
        $median = $row.Median
        $q3 = $row.Q3
        $max = $row.Max

        $insertQuery =  "insert into Tests ([RunId], [ContextId], [TestName], [N], [MeanMs], [StdDevMs], [MinMs], [Q1Ms], [MedianMs], [Q3Ms], [MaxMs]) "
        $insertQuery += "values ('$runId', '$contextId', '$testName', "

        if ($n.GetType() -eq [DBNull]) 
        {
            $insertQuery += "null" 
        } 
        else 
        { 
            $insertQuery += "'$n'" 
        } 

        $insertQuery += ", '$mean', '$stdDev', '$min', '$q1', '$median', '$q3', '$max'); select scope_identity() as 'Id'"
                
        $Command.CommandText = $insertquery
        $id = $Command.ExecuteScalar()

        [void]$ctxids.Add($id)
    }

    Write-Host "TestIds: " ([String]::Join(', ', $ctxids.ToArray()))
    Write-Host
}

Write-Host "--- End of script ---"
