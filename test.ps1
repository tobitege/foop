param(
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$project = Join-Path $root 'tests\Foop.Tests\Foop.Tests.csproj'
$logDirectory = Join-Path $root 'artifacts\logs'
$log = Join-Path $logDirectory 'test.log'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$arguments = @('run', '--project', $project, '--configuration', $Configuration, '--no-build')
dotnet @arguments 2>&1 | Tee-Object -FilePath $log
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Tests failed with exit code $exitCode. See $log."
}
