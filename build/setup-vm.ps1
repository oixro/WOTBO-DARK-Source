#Requires -RunAsAdministrator
<#
    One-time Windows 11 VM preparation for WOTBO development.

    Run inside the VM from an elevated PowerShell:
        Set-ExecutionPolicy Bypass -Scope Process -Force
        .\build\setup-vm.ps1 -SshPublicKey "ssh-ed25519 AAAA... user@mac"

    What it does:
      - enables long paths;
      - creates C:\src\WOTBO;
      - adds Defender exclusions for the workspace;
      - installs and starts OpenSSH Server;
      - optionally installs the SSH public key for the current admin user;
      - installs Visual Studio 2022 Build Tools with the .NET Framework 4.8
        targeting pack and Windows 11 SDK when MSBuild is missing.
#>
[CmdletBinding()]
param(
    [string]$WorkDir = 'C:\src\WOTBO',

    [string]$SshPublicKey = '',

    [switch]$SkipBuildTools
)

$ErrorActionPreference = 'Stop'

$script:Warnings = New-Object System.Collections.Generic.List[string]

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

function Write-Warn([string]$Text) {
    Write-Host "[WARN] $Text" -ForegroundColor Yellow
    $script:Warnings.Add($Text) | Out-Null
}

Write-Step 'Checking environment'
$os = Get-CimInstance Win32_OperatingSystem
$arch = $env:PROCESSOR_ARCHITECTURE
Write-Host "OS   : $($os.Caption) $($os.Version) $($os.BuildNumber)"
Write-Host "Arch : $arch"

Write-Step 'Checking connectivity'
foreach ($target in 'www.microsoft.com', 'github.com') {
    $ok = Test-NetConnection -ComputerName $target -Port 443 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($ok) {
        Write-Host "  $target : 443 reachable" -ForegroundColor Green
    } else {
        Write-Warn "$target : 443 unreachable (Windows Update, OpenSSH MSI and Build Tools downloads will fail)"
    }
}

Write-Step 'Enabling long paths'
New-ItemProperty -Path 'HKLM:\SYSTEM\CurrentControlSet\Control\FileSystem' `
    -Name LongPathsEnabled -Value 1 -PropertyType DWord -Force | Out-Null

Write-Step "Creating workspace $WorkDir"
New-Item -ItemType Directory -Force -Path $WorkDir | Out-Null

Write-Step 'Adding Defender exclusions'
try {
    Add-MpPreference -ExclusionPath $WorkDir -ErrorAction Stop
    Write-Host "Excluded: $WorkDir"
} catch {
    Write-Warning "Could not add Defender exclusions: $($_.Exception.Message)"
}

Write-Step 'Configuring high performance power plan (best effort)'
try {
    powercfg.exe /setactive 8c5e7fda-e8bf-4a96-9a85-a6e23a8c635c 2>$null
} catch {
    Write-Warning 'High performance power plan is not available on this VM.'
}

Write-Step 'Installing OpenSSH Server'

<#
    Add-WindowsCapability тянет компонент из Windows Update и падает с
    0x80240438, если источник компонентов выключен политикой, WSUS-ом или
    деблоат-скриптом. Стратегия:
      1) capability как есть;
      2) разрешить Windows Update как источник и повторить;
      3) fallback: Win32-OpenSSH MSI с GitHub (работает всегда, есть ARM64-сборка).
#>

function Install-OpenSshCapability {
    $capability = Get-WindowsCapability -Online -Name 'OpenSSH.Server*' |
        Where-Object { $_.Name -like 'OpenSSH.Server*' } |
        Select-Object -First 1

    if (-not $capability) {
        throw 'capability OpenSSH.Server не найдена в образе'
    }
    if ($capability.State -eq 'Installed') {
        Write-Host "Уже установлен: $($capability.Name)"
        return
    }

    Write-Host "Installing $($capability.Name)..."
    Add-WindowsCapability -Online -Name $capability.Name -ErrorAction Stop | Out-Null
}

function Enable-WindowsUpdateAsComponentSource {
    $servicing = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Servicing'
    if (-not (Test-Path $servicing)) { New-Item -Path $servicing -Force | Out-Null }
    New-ItemProperty -Path $servicing -Name UseWindowsUpdateForOptionalComponents `
        -Value 1 -PropertyType DWord -Force | Out-Null

    $wu = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate'
    if (-not (Test-Path $wu)) { New-Item -Path $wu -Force | Out-Null }
    New-ItemProperty -Path $wu -Name DoNotConnectToWindowsUpdateInternetLocations `
        -Value 0 -PropertyType DWord -Force | Out-Null

    $au = Join-Path $wu 'AU'
    if (-not (Test-Path $au)) { New-Item -Path $au -Force | Out-Null }
    New-ItemProperty -Path $au -Name UseWUServer -Value 0 -PropertyType DWord -Force | Out-Null

    foreach ($service in 'wuauserv', 'bits') {
        try {
            Set-Service -Name $service -StartupType Manual -ErrorAction Stop
            Restart-Service -Name $service -Force -ErrorAction Stop
            Write-Host "  $service restarted"
        } catch {
            Write-Host "  $service : $($_.Exception.Message)" -ForegroundColor DarkGray
        }
    }
}

function Install-OpenSshMsi {
    $assetArch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'ARM64' } else { 'Win64' }
    $pattern = '^OpenSSH-' + $assetArch + '-v[\d.]+\.msi$'

    Write-Host "Ищу OpenSSH-$assetArch MSI на GitHub..."
    $releases = Invoke-RestMethod -UseBasicParsing `
        -Uri 'https://api.github.com/repos/PowerShell/Win32-OpenSSH/releases?per_page=10'

    foreach ($release in $releases) {
        $asset = $release.assets | Where-Object { $_.name -match $pattern } | Select-Object -First 1
        if (-not $asset) { continue }

        $msi = Join-Path $env:TEMP $asset.name
        Write-Host "Скачиваю $($asset.name) ($($release.tag_name))..."
        Invoke-WebRequest -UseBasicParsing -Uri $asset.browser_download_url -OutFile $msi

        & msiexec.exe /i $msi /qn /norestart
        if ($LASTEXITCODE -eq 0 -or $LASTEXITCODE -eq 3010) {
            Write-Host "Установлен $($asset.name)"
            return
        }
        Write-Host "MSI вернул $LASTEXITCODE, пробую следующую версию." -ForegroundColor Yellow
    }

    throw 'не удалось установить Win32-OpenSSH MSI'
}

$sshReady = $false

try {
    Install-OpenSshCapability
    $sshReady = $true
} catch {
    Write-Host "Add-WindowsCapability: $($_.Exception.Message)" -ForegroundColor Yellow
    Write-Host 'Разрешаю Windows Update как источник компонентов и повторяю...' -ForegroundColor Yellow
    try {
        Enable-WindowsUpdateAsComponentSource
        Install-OpenSshCapability
        $sshReady = $true
    } catch {
        Write-Host "Повторная попытка: $($_.Exception.Message)" -ForegroundColor Yellow
    }
}

if (-not $sshReady) {
    try {
        Install-OpenSshMsi
        $sshReady = $true
    } catch {
        Write-Warn "OpenSSH не установлен: $($_.Exception.Message)"
    }
}

if ($sshReady) {
    try {
        Set-Service -Name sshd -StartupType Automatic -ErrorAction Stop
        Start-Service -Name sshd -ErrorAction Stop
        Write-Host 'sshd запущен.' -ForegroundColor Green
    } catch {
        # MSI-установка иногда приходит без host keys: sshd тогда не стартует.
        $keygen = Join-Path $env:ProgramFiles 'OpenSSH\ssh-keygen.exe'
        if (Test-Path $keygen) {
            Write-Host 'Генерирую host keys...' -ForegroundColor Yellow
            & $keygen -A | Out-Null
            try {
                Start-Service -Name sshd -ErrorAction Stop
                Write-Host 'sshd запущен.' -ForegroundColor Green
            } catch {
                Write-Warn "sshd не запустился: $($_.Exception.Message)"
            }
        } else {
            Write-Warn "sshd не запустился: $($_.Exception.Message)"
        }
    }

    if (-not (Get-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' -ErrorAction SilentlyContinue)) {
        New-NetFirewallRule -Name 'OpenSSH-Server-In-TCP' `
            -DisplayName 'OpenSSH Server (sshd)' `
            -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22 | Out-Null
    }
}

# DefaultShell намеренно НЕ меняем: OpenSSH должен оставить cmd.exe.
# build/sync.sh и build/remote-build.sh рассчитаны на cmd.exe и пути без пробелов.

if ($SshPublicKey) {
    Write-Step 'Installing SSH public key'
    try {
        & (Join-Path $PSScriptRoot 'install-ssh-key.ps1') -SshPublicKey $SshPublicKey
    } catch {
        Write-Warn "Ключ не установлен: $($_.Exception.Message)"
    }
} else {
    Write-Warn 'No -SshPublicKey supplied. Add the key later with build\install-ssh-key.ps1.'
}

function Find-MSBuild {
    $programFilesX86 = [Environment]::GetFolderPath('ProgramFilesX86')
    $vswhere = Join-Path $programFilesX86 'Microsoft Visual Studio\Installer\vswhere.exe'
    if (Test-Path $vswhere) {
        return (& $vswhere -latest -products * -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' |
            Select-Object -First 1)
    }
    return $null
}

$msbuild = Find-MSBuild

function Install-BuildTools([bool]$WithSdk) {
    $installer = Join-Path $env:TEMP 'vs_buildtools.exe'
    Invoke-WebRequest -Uri 'https://aka.ms/vs/17/release/vs_buildtools.exe' `
        -OutFile $installer -UseBasicParsing

    $arguments = @(
        '--quiet',
        '--wait',
        '--norestart',
        '--nocache',
        '--installPath', 'C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools',
        '--add', 'Microsoft.VisualStudio.Workload.ManagedDesktopBuildTools',
        '--add', 'Microsoft.Net.Component.4.8.TargetingPack'
    )
    if ($WithSdk) {
        $arguments += @('--add', 'Microsoft.VisualStudio.Component.Windows11SDK.22621')
    }
    $arguments += '--includeRecommended'

    # ВАЖНО: вызываем установщик напрямую, а не через
    # Start-Process -ArgumentList. Там массив аргументов склеивается пробелами
    # без кавычек, и путь 'C:\Program Files (x86)\...' разваливается:
    # установщик получает --installPath C:\Program и ставит VS в C:\Program.
    & $installer @arguments
    return $LASTEXITCODE
}

if (-not $msbuild -and -not $SkipBuildTools) {
    Write-Step 'Installing Visual Studio 2022 Build Tools'
    if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') {
        Write-Host 'ARM64 host: the installer and MSBuild run under x64 emulation.' -ForegroundColor Yellow
    }

    # Windows 11 SDK is optional for this project: WOTBO has no C++ code and no
    # WinRT references, so the .NET Framework 4.8 targeting pack is enough.
    # The SDK component is not available for every architecture, so retry
    # without it if the first attempt fails.
    $exitCode = Install-BuildTools -WithSdk $true
    if ($exitCode -ne 0 -and $exitCode -ne 3010) {
        Write-Warning "Build Tools installer exited with code $exitCode. Retrying without the Windows 11 SDK component..."
        $exitCode = Install-BuildTools -WithSdk $false
    }

    if ($exitCode -ne 0 -and $exitCode -ne 3010) {
        Write-Warning "Build Tools installer exited with code $exitCode. Install the components manually if MSBuild is missing."
    }

    $msbuild = Find-MSBuild
}

if ($msbuild) {
    Write-Host "MSBuild: $msbuild" -ForegroundColor Green
} else {
    Write-Warning 'MSBuild was not found. Install Visual Studio 2022 Build Tools manually.'
}

Write-Step 'Summary'
Write-Host "Workspace : $WorkDir"
Write-Host "Hostname  : $env:COMPUTERNAME"

$sshd = Get-Service -Name sshd -ErrorAction SilentlyContinue
if ($sshd -and $sshd.Status -eq 'Running') {
    Write-Host 'sshd      : Running' -ForegroundColor Green
} else {
    Write-Host 'sshd      : not running' -ForegroundColor Red
}

if ($msbuild) {
    Write-Host "MSBuild   : $msbuild"
} else {
    Write-Host 'MSBuild   : not found' -ForegroundColor Red
}

if ($script:Warnings.Count -gt 0) {
    Write-Host ''
    Write-Host 'Warnings:' -ForegroundColor Yellow
    foreach ($warning in $script:Warnings) {
        Write-Host "  - $warning" -ForegroundColor Yellow
    }
}

Write-Host ''
Write-Host 'Next:'
Write-Host '  .\build\doctor.ps1'
Write-Host ''
Write-Host 'SSH test from macOS:'
Write-Host "  ssh $env:USERNAME@<vm-ip> `"powershell -NoProfile -Command `$PSVersionTable.PSVersion`""
Write-Host 'Build from macOS:'
Write-Host '  export WOTBO_VM_HOST=<vm-ip>'
Write-Host '  export WOTBO_VM_USER=<windows-user>'
Write-Host '  ./build/remote-build.sh'
Write-Host ''
Write-Host 'Create a VM snapshot named "P0-clean" before running tests.' -ForegroundColor Yellow
