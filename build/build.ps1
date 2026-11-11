#Requires -Version 5.1
<#
    Build WOTBO (classic .NET Framework 4.8, x64) on Windows.
    Usage:
        powershell -ExecutionPolicy Bypass -File .\build\build.ps1
        powershell -ExecutionPolicy Bypass -File .\build\build.ps1 -Configuration Debug -Clean
#>
[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean
)

$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$solution = Join-Path $root 'Project.sln'
$artifacts = Join-Path $root 'artifacts'
$logs = Join-Path $artifacts 'logs'
$output = Join-Path $root "bin\$Configuration"
$destination = Join-Path $artifacts "x64\$Configuration"

New-Item -ItemType Directory -Force -Path $logs | Out-Null

$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$log = Join-Path $logs "build-$Configuration-$stamp.log"

function Find-MSBuild {
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    $vswhere = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'

    if (Test-Path $vswhere) {
        $candidate = & $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1
        if ($candidate -and (Test-Path $candidate)) {
            return $candidate
        }
    }

    $command = Get-Command msbuild.exe -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    $dotnet = Get-Command dotnet.exe -ErrorAction SilentlyContinue
    if ($dotnet) {
        return $dotnet.Source
    }

    throw 'MSBuild not found. Install Visual Studio 2022 Build Tools with "Managed Desktop Build Tools" and the .NET Framework 4.8 targeting pack.'
}

$msbuild = Find-MSBuild
$isDotnet = [IO.Path]::GetFileName($msbuild) -ieq 'dotnet.exe'

$msbuildArgs = @(
    $solution
    '/m'
    ('/p:Configuration={0}' -f $Configuration)
    '/p:Platform=x64'
    '/v:minimal'
    ('/flp:LogFile={0};Verbosity=detailed' -f $log)
    ('/t:{0}' -f $(if ($Clean) { 'Rebuild' } else { 'Build' }))
)

Write-Host "MSBuild : $msbuild"
Write-Host "Solution: $solution"
Write-Host "Config  : $Configuration|x64"
Write-Host "Log     : $log"
Write-Host "Args    : $($msbuildArgs -join ' ')"

if ($isDotnet) {
    & $msbuild msbuild @msbuildArgs
} else {
    & $msbuild @msbuildArgs
}

if ($LASTEXITCODE -ne 0) {
    throw "Build failed with exit code $LASTEXITCODE. See $log"
}

$exe = Join-Path $output 'WOTBO.exe'
if (-not (Test-Path $exe)) {
    throw "Build succeeded, but $exe was not found."
}

$bytes = [IO.File]::ReadAllBytes($exe)
$peOffset = [BitConverter]::ToInt32($bytes, 0x3C)
$machine = [BitConverter]::ToUInt16($bytes, $peOffset + 4)
if ($machine -ne 0x8664) {
    throw ('Expected an x64 PE image (0x8664), got 0x{0:X4}. Check PlatformTarget/Platform.' -f $machine)
}
Write-Host 'PE machine: 0x8664 (x64)'

New-Item -ItemType Directory -Force -Path $destination | Out-Null
Get-ChildItem -Path $output -File | Copy-Item -Destination $destination -Force

Write-Host ''
Write-Host "OK: $(Join-Path $destination 'WOTBO.exe')" -ForegroundColor Green
Write-Host "Log: $log"
