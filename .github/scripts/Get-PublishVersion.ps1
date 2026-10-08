param(
    [string]$RefType = $env:GITHUB_REF_TYPE,
    [string]$RefName = $env:GITHUB_REF_NAME
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($RefType -notin @('branch', 'tag') -or [string]::IsNullOrWhiteSpace($RefName)) {
    throw 'A GitHub branch or tag reference is required.'
}
$versions = @()
foreach ($project in @('src/Lumis/Lumis.csproj', 'src/Lumis.Security/Lumis.Security.csproj')) {
    $output = & dotnet msbuild $project -nologo -getProperty:PackageVersion
    if ($LASTEXITCODE -ne 0) { throw "Could not evaluate package version: $project" }
    $value = ($output -join "`n").Trim()
    if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains("`n")) {
        throw "MSBuild did not return a single version: $project"
    }
    $versions += $value
}
if ($versions[0] -cne $versions[1]) {
    throw 'LumisAPI and Lumis.Security must have matching release package versions.'
}
$version = $versions[0]
if ($RefType -eq 'tag' -and $RefName -cne "v$version") {
    throw "Release tag '$RefName' does not match package version '$version'. Expected 'v$version'."
}
Write-Output $version
