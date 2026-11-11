#Requires -Version 5.1
<#
    Run the built WOTBO.exe elevated and copy %TEMP%\wotbo.log back to artifacts\logs.
    Usage:
        powershell -ExecutionPolicy Bypass -File .\build\run.ps1
        powershell -ExecutionPolicy Bypass -File .\build\run.ps1 -Configuration Debug -NoWait
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$NoWait
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root "artifacts\x64\$Configuration\WOTBO.exe"
$logs = Join-Path $root 'artifacts\logs'

if (-not (Test-Path $exe)) {
    throw "WOTBO.exe not found: $exe. Run build\build.ps1 first."
}

New-Item -ItemType Directory -Force -Path $logs | Out-Null

$identity = [Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object Security.Principal.WindowsPrincipal($identity)
$isAdmin = $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Write-Host "Starting: $exe"
if ($isAdmin) {
    $process = Start-Process -FilePath $exe -PassThru
} else {
    Write-Host 'Requesting administrator rights...'
    $process = Start-Process -FilePath $exe -Verb RunAs -PassThru
}

if ($NoWait) {
    Write-Host "Started PID $($process.Id)."
    return
}

$process.WaitForExit()
try {
    Write-Host "Exit code: $($process.ExitCode)"
} catch {
    # Non-elevated parent may not be allowed to read the exit code of an
    # elevated child; not a failure by itself.
    Write-Host 'Exit code: unavailable'
}

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$candidates = @(
    (Join-Path $env:TEMP 'wotbo.log'),
    (Join-Path $env:TEMP 'wotbo.1.log'),
    'C:\Windows\Temp\wotbo.log'
) | Select-Object -Unique

foreach ($source in $candidates) {
    if (Test-Path $source) {
        $target = Join-Path $logs "wotbo-$stamp.log"
        Copy-Item -Path $source -Destination $target -Force
        Write-Host "Log: $target" -ForegroundColor Green
        break
    }
}

if (-not (Test-Path (Join-Path $logs "wotbo-$stamp.log"))) {
    Write-Warning "wotbo.log was not found in TEMP."
}
