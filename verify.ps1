param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$buildScript = Join-Path $root 'build.ps1'
$testScript = Join-Path $root 'test.ps1'

& $buildScript -Configuration $Configuration
& $testScript -Configuration $Configuration
