
# =================================================================================
# Replaces grammar productions in a .md file from a .grammar file
#

$ErrorActionPreference = "Stop"

$gpath = "expression-grammar.grammar"
$mdpath = "expression-grammar.md"
$backup = "c:\temp\powerfx-expression-grammar-backup.md"

$prods = @{}

$original = Get-Content -Path $mdpath
$out = ""

#try 
#{
	$grammar = Get-Content -Path $gpath
	$grammar = $grammar + "`r`n"

	$names = @{}
	$used = @{}

	# =========================================================================================
	# first pass, register names
	#
	ForEach( $line in $($grammar -split "`r`n"))
	{
		if( $line -match '^(\w+)\s*:' )
		{
			if( $names[$matches[1]] )
			{
				write-host "grammar: duplicate name:" $matches[1] -ForegroundColor Red
			}
			$names[$matches[1]] = $true
		}
	}

	# =========================================================================================
	# second pass, read productions
	#
	$md = ""
	ForEach ($line in $($grammar -split "`r`n"))
	{
		$line = if( $line -match '^(.*[^``])\/\/' ) { $matches[1] } else { $line }

		if( $line -match '^(\w+)\s*:\s*(one\s+of)?' )
		{
			$name = $matches[1]
			$after = if( $matches[2].length -gt 4 ) { " **one of**"} else { "" }
			$md = '&emsp;&emsp;<a name="' + $name + '"></a>*' + $name + "* **:**" + $after + "<br>`r`n"
		}
		elseif( $line -match '^\s*$' )
		{
			if( $md -ne "" )
			{	
				$prods[ $name ] = $md
			}
			$md = ""
		}
		elseif( $line -match '^\s+>\s*(.*)$') 
		{
			$line = $matches[1]
			$md = $md + '&emsp;&emsp;&emsp;&emsp;'

			$spacing = ""
			ForEach ($word in $($line -split "\s+"))
			{
				if( $names[$word] )
				{
					$md = $md + $spacing + '*[' + $word + '](#' + $word + ')*'
					$used[$word]++
				}
				else 
				{
					$md = $md + $spacing + $word	
				}
				$spacing = " "
			}			
			$md = $md + "<br>`r`n"
		}
		elseif( $line -match '^\s+[^\s]' )
		{
			$md = $md + '&emsp;&emsp;&emsp;'
			$spacing = "&emsp;"
			ForEach ($word in $($line -split "\s+"))
			{
				if( $word -match '^\w' )
				{
					if( $word -eq "but" -or $word -eq "not" -or $word -eq "or" -or $word -eq "one" )
					{
						$md = $md + ' **' + $word + '**'
						$spacing = " "
					}
					else 
					{
						if( $word -match '^(.*)\?$')
						{
							$word = $matches[1]
							$opt = "<sub>opt</sub>"
						}
						else 
						{
							$opt = ""
						}
						if( -not $names[$word] )
						{
							write-host "grammar: name not found:" $word -ForegroundColor Red
						}
						$used[$word]++
						$md = $md + $spacing + '*[' + $word + '](#' + $word + ')*' + $opt
					}
				}
				elseif( $word -match '[^\s]')
				{
					$md = $md + '&emsp;' + $word 
				}
			}
			$md = $md + "<br>`r`n"
		}
	}	

	# =========================================================================================
	# scan for productions not used
	#
	ForEach( $word in $names.Keys )
	{
		if( $used[$word] -lt 1 )
		{
			write-host "grammar: name not used:" $word -ForegroundColor Red
		}
	}

	# =========================================================================================
	# replace in md file
	#
	$eating = $false
	$mdused = @{}
	ForEach ($line in $($original -split "`r`n"))
	{
		if( $line -imatch '^\#.*conventions' )
		{
			$conventions = $true
		}
		elseif( $line -match '^\#')
		{
			$conventions = $false
		}

		if( $line -imatch '^&emsp;&emsp;.*\*(\w+)\*[\s\*]*:' -and -not $conventions )
		{
			$name = $matches[1]
			if( $names[ $name ] )
			{
				$out += $prods[ $name ] + "`r`n"
				$eating = $true
				$mdused[ $name ]++
			}
			else 
			{
				write-host "md: name not found:" $name -ForegroundColor Red
			}
		}
		elseif( $eating -and $line -match '^[^&]' )
		{
			$eating = $false
			$out += "$line`r`n"			
		}
		elseif( -not $eating )
		{
			$out += "$line`r`n"
		}
	}

	# =========================================================================================
	# scan md used
	#
	ForEach( $word in $names.Keys )
	{
		if( $mdused[$word] -gt 1 )
		{
			write-host "md: name used more than once:" $word -ForegroundColor Red
		}
		elseif( $mdused[$word] -lt 1 )
		{
			write-host "md: name not used:" $word -ForegroundColor Red
		}
	}
#} 
#catch
#{
#	write-host "Caught an exception:" -ForegroundColor Red
	#write-host "  Type: $($_.Exception.GetType().FullName)" -ForegroundColor Red
	#write-host "  Message: $($_.Exception.Message)" -ForegroundColor Red
	#write-host "  StackTrace: $($_.Exception.StackTrace)" -ForegroundColor Red
	#break;
#}

Set-Content -Path $backup $original

Set-Content -Path $mdpath $out




