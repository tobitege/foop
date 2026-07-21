$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot 'tests\Foop.Tests\Foop.Tests.csproj'
$logDirectory = Join-Path $repoRoot 'artifacts\logs'
$log = Join-Path $logDirectory 'test.log'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$arguments = @('run', '--project', $project, '--configuration', 'Release', '--no-build')
dotnet @arguments 2>&1 | Tee-Object -FilePath $log
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Tests failed with exit code $exitCode. See $log."
}
