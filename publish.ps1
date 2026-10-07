<#
.SYNOPSIS
    Packs every library under src/ into the local NuGet feed.

.DESCRIPTION
    NuGet caches a package by id and version (%UserProfile%\.nuget\packages): publishing the same version
    twice would leave apps on the stale copy. The script therefore refuses to overwrite a version already in
    the feed; pass -Bump to increment the patch number of <Version> in Directory.Build.props first.

.EXAMPLE
    .\publish.ps1 -Bump
#>
param(
    [switch] $Bump,
    [string] $Feed = (Join-Path $env:USERPROFILE 'nuget_local')
)

$ErrorActionPreference = 'Stop'
$props = Join-Path $PSScriptRoot 'Directory.Build.props'
$text = [IO.File]::ReadAllText($props)
$match = [regex]::Match($text, '<Version>(\d+)\.(\d+)\.(\d+)</Version>')
if (-not $match.Success) {
    throw 'Directory.Build.props has no <Version>major.minor.patch</Version>.'
}

$major, $minor, $patch = [int]$match.Groups[1].Value, [int]$match.Groups[2].Value, [int]$match.Groups[3].Value
if ($Bump) {
    $patch++
    $text = $text.Remove($match.Index, $match.Length).Insert($match.Index, "<Version>$major.$minor.$patch</Version>")
    [IO.File]::WriteAllText($props, $text, (New-Object Text.UTF8Encoding $false))
}

$version = "$major.$minor.$patch"
$projects = Get-ChildItem (Join-Path $PSScriptRoot 'src') -Recurse -Filter *.csproj
foreach ($project in $projects) {
    $package = Join-Path $Feed "$($project.BaseName).$version.nupkg"
    if (Test-Path $package) {
        throw "$package already exists. Run .\publish.ps1 -Bump to publish a new version."
    }
}

New-Item -ItemType Directory -Force $Feed | Out-Null
foreach ($project in $projects) {
    dotnet pack $project.FullName -c Release -o $Feed --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet pack failed for $($project.Name)."
    }
}

Write-Host "Published $version to $Feed"
