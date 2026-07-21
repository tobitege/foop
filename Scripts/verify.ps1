$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $repoRoot 'build.ps1'
$testScript = Join-Path $PSScriptRoot 'test.ps1'

& $buildScript
& $testScript
