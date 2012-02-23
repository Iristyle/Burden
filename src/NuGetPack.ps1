param(
	[Parameter(Mandatory = $true, ValueFromPipeline = $true)]
	[string]
	$apiKey
)

function Pack-And-Push
{
    $thisName = $MyInvocation.MyCommand.Name
    $currentDirectory = [IO.Path]::GetDirectoryName((Get-Content function:$thisName).File)
    Write-Host "Running against $currentDirectory"
    $nuget = Get-ChildItem -Path $currentDirectory -Include 'nuget.exe' -Recurse |
        Select -ExpandProperty FullName -First 1

    Get-ChildItem -Path $currentDirectory -Include *.nuspec -Recurse | 
        % { Join-Path ([IO.Path]::GetDirectoryName($_)) ([IO.Path]::GetFileNameWithoutExtension($_) + '.csproj') } |
        ? { Test-Path $_ } |
        % { Start-Process $nuget -ArgumentList "pack $_ -Build -Prop Configuration=Release -Exclude '**\*.CodeAnalysisLog.xml'" -NoNewWindow -Wait }
    
    Get-ChildItem *.nupkg | % { &$nuget push $_ $apiKey }     
}

del *.nupkg
Pack-And-Push
del *.nupkg