#Requires -RunAsAdministrator
<#
    Install an SSH public key for the current Windows user and restart sshd.

    Run inside the VM from an elevated PowerShell:
        Set-ExecutionPolicy Bypass -Scope Process -Force
        .\build\install-ssh-key.ps1 -SshPublicKey "ssh-ed25519 AAAA... user@mac"

    The key is written to:
      - C:\ProgramData\ssh\administrators_authorized_keys (used for members of
        the Administrators group; needs a strict ACL, otherwise sshd ignores it);
      - %USERPROFILE%\.ssh\authorized_keys (used for normal accounts).

    Idempotent: running it twice does not duplicate the key.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$SshPublicKey
)

$ErrorActionPreference = 'Stop'

function Write-Step([string]$Text) {
    Write-Host ''
    Write-Host "==> $Text" -ForegroundColor Cyan
}

$key = $SshPublicKey.Trim()
if ($key -notmatch '^(ssh-(rsa|ed25519)|ecdsa-sha2-nistp\d+|sk-(ssh-ed25519|ecdsa-sha2-nistp256))@openssh\.com\s+\S+' -and
    $key -notmatch '^(ssh-(rsa|ed25519)|ecdsa-sha2-nistp\d+|sk-(ssh-ed25519|ecdsa-sha2-nistp256))\s+\S+') {
    throw "Не похоже на публичный ключ SSH. Ожидается строка вида 'ssh-ed25519 AAAA... comment'. Получено: $key"
}

Write-Step 'Installing key for Administrators'

$adminKeys = Join-Path $env:ProgramData 'ssh\administrators_authorized_keys'
New-Item -ItemType Directory -Force -Path (Split-Path $adminKeys) | Out-Null

$lines = @()
if (Test-Path $adminKeys) {
    $lines = @(Get-Content -Path $adminKeys | Where-Object { $_.Trim() })
}

$alreadyPresent = $lines | Where-Object { $_.Trim() -eq $key }
if (-not $alreadyPresent) {
    $lines += $key
    Set-Content -Path $adminKeys -Value $lines -Encoding ascii
    Write-Host "Added to $adminKeys"
} else {
    Write-Host "Already present in $adminKeys"
}

# sshd on Windows ignores this file for administrators unless the ACL is
# restricted to Administrators and SYSTEM, with inheritance disabled.
icacls.exe $adminKeys /inheritance:r /grant '*S-1-5-32-544:F' /grant '*S-1-5-18:F' | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "icacls failed with exit code $LASTEXITCODE"
}

Write-Step 'Installing key for the current user'
$userKeys = Join-Path $HOME '.ssh\authorized_keys'
New-Item -ItemType Directory -Force -Path (Split-Path $userKeys) | Out-Null

$userLines = @()
if (Test-Path $userKeys) {
    $userLines = @(Get-Content -Path $userKeys | Where-Object { $_.Trim() })
}
if (-not ($userLines | Where-Object { $_.Trim() -eq $key })) {
    $userLines += $key
    Set-Content -Path $userKeys -Value $userLines -Encoding ascii
    Write-Host "Added to $userKeys"
} else {
    Write-Host "Already present in $userKeys"
}

Write-Step 'Checking sshd configuration'
$sshdConfig = Join-Path $env:ProgramData 'ssh\sshd_config'
if (-not (Test-Path $sshdConfig)) {
    Write-Warning "$sshdConfig not found. OpenSSH Server may not be configured correctly."
} else {
    $config = Get-Content -Path $sshdConfig -Raw
    if ($config -notmatch '(?m)^\s*PubkeyAuthentication\s+yes') {
        Write-Host 'PubkeyAuthentication не задан явно (по умолчанию yes).'
    }
    if ($config -notmatch 'administrators_authorized_keys') {
        Write-Warning 'В sshd_config нет Match-блока для administrators_authorized_keys. Ключ администратора может игнорироваться.'
    }
}

Write-Step 'Restarting sshd'
Set-Service -Name sshd -StartupType Automatic
Restart-Service -Name sshd
Start-Sleep -Seconds 1

$sshd = Get-Service -Name sshd
Write-Host "sshd: $($sshd.Status)"
if ($sshd.Status -ne 'Running') {
    throw 'sshd is not running.'
}

$listening = Get-NetTCPConnection -State Listen -LocalPort 22 -ErrorAction SilentlyContinue
if ($listening) {
    Write-Host 'Port 22: listening' -ForegroundColor Green
} else {
    Write-Warning 'Nothing is listening on port 22.'
}

$ip = (Get-NetIPAddress -AddressFamily IPv4 |
    Where-Object { $_.IPAddress -notlike '127.*' -and $_.PrefixOrigin -ne 'WellKnown' } |
    Select-Object -First 1).IPAddress

Write-Host ''
Write-Host 'Done. Test from macOS:' -ForegroundColor Green
Write-Host "  ssh $env:USERNAME@$ip `"powershell -NoProfile -Command `$PSVersionTable.PSVersion`""
