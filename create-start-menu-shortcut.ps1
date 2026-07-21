[CmdletBinding(SupportsShouldProcess)]
param(
    [ValidateSet('CurrentUser', 'AllUsers')]
    [string] $Scope = 'CurrentUser'
)

$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$portableExecutable = Join-Path $root 'Foop.exe'
$developmentExecutable = Join-Path $root 'src\Foop\bin\Release\net10.0-windows\Foop.exe'
$executable = if (Test-Path -LiteralPath $portableExecutable -PathType Leaf) {
    $portableExecutable
}
else {
    $developmentExecutable
}

if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    throw "Foop.exe was not found beside this script or at '$developmentExecutable'. Build Foop first."
}

$programsFolder = if ($Scope -eq 'AllUsers') {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::CommonPrograms)
}
else {
    [Environment]::GetFolderPath([Environment+SpecialFolder]::Programs)
}

if ([string]::IsNullOrWhiteSpace($programsFolder)) {
    throw "Windows did not provide a Start menu folder for scope '$Scope'."
}

$shortcutPath = Join-Path $programsFolder 'Foop.lnk'
$action = "Create the Foop Start menu shortcut for scope '$Scope'"
if (-not $PSCmdlet.ShouldProcess($shortcutPath, $action)) {
    return
}

if ($Scope -eq 'AllUsers') {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    $administratorRole = [Security.Principal.WindowsBuiltInRole]::Administrator
    if (-not $principal.IsInRole($administratorRole)) {
        throw 'Creating the shortcut for all users requires an elevated PowerShell session.'
    }
}

$workingDirectory = Split-Path -Path $executable -Parent
$shell = $null
$shortcut = $null
try {
    $shell = New-Object -ComObject WScript.Shell
    $shortcut = $shell.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $executable
    $shortcut.WorkingDirectory = $workingDirectory
    $shortcut.Description = 'Foop – Fenster auf den aktuellen Monitor holen'
    $shortcut.IconLocation = "$executable,0"
    $shortcut.Save()
}
finally {
    if ($null -ne $shortcut) {
        [void] [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shortcut)
    }

    if ($null -ne $shell) {
        [void] [Runtime.InteropServices.Marshal]::FinalReleaseComObject($shell)
    }
}

Write-Host "Start menu shortcut created: $shortcutPath"
