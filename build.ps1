param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$solution = Join-Path $root 'Foop.slnx'
$logDirectory = Join-Path $root 'artifacts\logs'
$log = Join-Path $logDirectory 'build.log'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$arguments = @('build', $solution, '--configuration', $Configuration, '--nologo')
dotnet @arguments 2>&1 | Tee-Object -FilePath $log
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Build failed with exit code $exitCode. See $log."
}
