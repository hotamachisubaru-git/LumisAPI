param(
    [string]$RefType = $env:GITHUB_REF_TYPE,
    [string]$RefName = $env:GITHUB_REF_NAME
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($RefType -notin @('branch', 'tag') -or [string]::IsNullOrWhiteSpace($RefName)) {
    throw 'A GitHub branch or tag reference is required.'
}

$versionOutput = & dotnet msbuild src/Lumis/Lumis.csproj -nologo -getProperty:PackageVersion
if ($LASTEXITCODE -ne 0) {
    throw 'Could not evaluate the NuGet package version.'
}
$version = ($versionOutput -join "`n").Trim()
if ([string]::IsNullOrWhiteSpace($version) -or $version.Contains("`n")) {
    throw 'MSBuild did not return a single package version.'
}

if ($RefType -eq 'tag' -and $RefName -cne "v$version") {
    throw "Release tag '$RefName' does not match package version '$version'. Expected 'v$version'."
}

Write-Output $version
