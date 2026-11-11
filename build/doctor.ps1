#Requires -Version 5.1
<#
    Diagnostics for the Windows development VM.
    Run inside the VM:
        powershell -ExecutionPolicy Bypass -File .\build\doctor.ps1
#>
[CmdletBinding()]
param(
    [string]$WorkDir = 'C:\src\WOTBO'
)

$ErrorActionPreference = 'Continue'
$failures = New-Object System.Collections.Generic.List[string]

# Over SSH Windows PowerShell writes OEM codepage by default, which turns
# Cyrillic and box characters into '?'. Force UTF-8 for SSH sessions only.
if ($env:SSH_CONNECTION) {
    try { [Console]::OutputEncoding = [Text.Encoding]::UTF8 } catch { }
}

function Check([string]$Name, [bool]$Ok, [string]$Details = '') {
    if ($Ok) {
        Write-Host ("[ OK ] {0} {1}" -f $Name, $Details) -ForegroundColor Green
    } else {
        Write-Host ("[FAIL] {0} {1}" -f $Name, $Details) -ForegroundColor Red
        $failures.Add($Name) | Out-Null
    }
}

$os = Get-CimInstance Win32_OperatingSystem
$isAdmin = (New-Object Security.Principal.WindowsPrincipal(
    [Security.Principal.WindowsIdentity]::GetCurrent())
).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)

Write-Host 'WOTBO VM doctor' -ForegroundColor Cyan
Write-Host "OS      : $($os.Caption) $($os.Version) build $($os.BuildNumber)"
Write-Host "Arch    : $env:PROCESSOR_ARCHITECTURE"
Write-Host "Host    : $env:COMPUTERNAME"
Write-Host "User    : $env:USERNAME"
Write-Host ''

Check 'Administrator' $isAdmin

$longPaths = (Get-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' -Name LongPathsEnabled -ErrorAction SilentlyContinue).LongPathsEnabled
Check 'LongPathsEnabled' ($longPaths -eq 1) "value=$longPaths"

Check 'Workspace exists' (Test-Path $WorkDir) $WorkDir

$programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
$vswhere = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
$msbuild = $null
if (Test-Path $vswhere) {
    $msbuild = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
        Select-Object -First 1
}
Check 'MSBuild' ([bool]$msbuild) ([string]$msbuild)

if ($msbuild -and $msbuild -notmatch 'Microsoft Visual Studio') {
    Write-Host '[WARN] MSBuild is outside the standard Visual Studio folder.' -ForegroundColor Yellow
    Write-Host "       $msbuild" -ForegroundColor Yellow
    Write-Host '       Cause: unquoted --installPath during Build Tools setup.' -ForegroundColor Yellow
    Write-Host '       Fix: docs/VM-SETUP.md, section 4.' -ForegroundColor Yellow
}

$release = (Get-ItemProperty -Path 'HKLM:\SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full' -Name Release -ErrorAction SilentlyContinue).Release
Check '.NET Framework 4.8+' ($release -ge 528040) "Release=$release"

$sshd = Get-Service -Name sshd -ErrorAction SilentlyContinue
Check 'OpenSSH service' ($sshd -and $sshd.Status -eq 'Running') $(if ($sshd) { $sshd.Status } else { 'not installed' })

$firewall = Get-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -ErrorAction SilentlyContinue
Check 'OpenSSH firewall rule' ([bool]$firewall)

Check 'tar.exe' ([bool](Get-Command tar.exe -ErrorAction SilentlyContinue))
Check 'PowerShell' ($PSVersionTable.PSVersion.Major -ge 5) $PSVersionTable.PSVersion.ToString()

$exe = Join-Path $WorkDir 'artifacts\x64\Release\WOTBO.exe'
if (Test-Path $exe) {
    $bytes = [IO.File]::ReadAllBytes($exe)
    $pe = [BitConverter]::ToInt32($bytes, 0x3C)
    $machine = [BitConverter]::ToUInt16($bytes, $pe + 4)
    Check 'WOTBO.exe is x64' ($machine -eq 0x8664) ('machine=0x{0:X4}' -f $machine)
} else {
    Write-Host '[INFO] WOTBO.exe not built yet.' -ForegroundColor Yellow
}

Write-Host ''
if ($failures.Count -eq 0) {
    Write-Host 'All checks passed.' -ForegroundColor Green
    exit 0
}

Write-Host ("Failed checks: {0}" -f ($failures -join ', ')) -ForegroundColor Red
exit 1
