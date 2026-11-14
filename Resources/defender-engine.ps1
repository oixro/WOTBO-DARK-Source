<#
    WOTBO Defender engine.

    Phases:
      prepare  - export registry, record files/tasks, write journal
      apply    - stop services, take ownership, rename files, disable services,
                 drivers, tasks, policies, firewall, SmartScreen, SAC, Security UI
      restore  - reverse everything recorded in the journal
      verify   - write verify.ini with the real Defender state
      watchdog - remove safeboot if the safe-mode stage did not finish

    The script is ASCII-only on purpose: Windows PowerShell 5.1 reads a
    BOM-less file as ANSI.
#>
param(
    [ValidateSet('prepare', 'apply', 'restore', 'verify', 'watchdog', 'finalize')]
    [string]$Phase = 'verify',
    [string]$Root = '',
    [switch]$SafeMode
)

$ErrorActionPreference = 'Continue'
$ProgressPreference = 'SilentlyContinue'

if ([string]::IsNullOrWhiteSpace($Root)) {
    $Root = Join-Path $env:ProgramData 'oixro\wotbo\defender'
}

$script:Root = $Root
$script:LogDir = Join-Path $Root 'logs'
$script:RegDir = Join-Path $Root 'reg'
$script:TaskDir = Join-Path $Root 'tasks'
$script:JournalPath = Join-Path $Root 'journal.json'
$script:StatePath = Join-Path $Root 'state.ini'
$script:VerifyPath = Join-Path $Root 'verify.ini'
$script:LogPath = Join-Path $script:LogDir ("{0}-{1}.log" -f $Phase, (Get-Date -Format 'yyyyMMdd-HHmmss'))
$script:PublicLog = Join-Path $env:WINDIR 'Temp\wotbo-defender.log'
$script:RunOnceKey = 'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce'
$script:RunOnceValue = 'WotboDefenderStage'
$script:Desktop = Join-Path $env:PUBLIC 'Desktop'

$script:StageTask = '\WOTBO\DefenderStage'
$script:WatchdogTask = '\WOTBO\DefenderWatchdog'

New-Item -ItemType Directory -Force -Path $Root, $script:LogDir, $script:RegDir, $script:TaskDir | Out-Null

$script:TaskNames = @(
    'Microsoft\Windows\Windows Defender\Windows Defender Cache Maintenance',
    'Microsoft\Windows\Windows Defender\Windows Defender Cleanup',
    'Microsoft\Windows\Windows Defender\Windows Defender Scheduled Scan',
    'Microsoft\Windows\Windows Defender\Windows Defender Verification',
    'Microsoft\Windows\AppID\SmartScreenSpecific'
)

$script:FixedServices = @(
    'WinDefend',
    'WdNisSvc',
    'WdNisDrv',
    'WdBoot',
    'WdFilter',
    'Sense',
    'SgrmBroker',
    'SgrmAgent',
    'MDCoreSvc',
    'MpDefenderCoreService',
    'MsSecFlt',
    'MsSecCore',
    'MsSecWfp',
    'SecurityHealthService',
    'MpsSvc',
    'webthreatdefsvc'
)

function Write-Log([string]$Message) {
    $line = ('[{0}] {1}' -f (Get-Date -Format 'HH:mm:ss'), $Message)
    try { Add-Content -LiteralPath $script:LogPath -Value $line -Encoding UTF8 } catch { }
    try { Add-Content -LiteralPath $script:PublicLog -Value $line -Encoding UTF8 } catch { }
    Write-Host $line
}

function Write-State([string]$Stage, [string]$Result, [string]$Message) {
    $mode = 'fast'
    if ($SafeMode) { $mode = 'safemode' }
    $lines = @(
        ('stage=' + $Stage),
        ('result=' + $Result),
        ('message=' + (($Message -replace '[\r\n]+', ' '))),
        ('mode=' + $mode),
        ('phase=' + $Phase),
        ('updated=' + (Get-Date).ToString('o'))
    )
    try { Set-Content -LiteralPath $script:StatePath -Value $lines -Encoding ASCII } catch { }
}

function Exec([string]$FilePath, [string[]]$Arguments) {
    Write-Log ('exec: ' + $FilePath + ' ' + ($Arguments -join ' '))
    $output = & $FilePath @Arguments 2>&1
    if ($output) {
        $output | ForEach-Object { Write-Log ('  ' + [string]$_) }
    }
    return $LASTEXITCODE
}

function Test-RegKey([string]$Key) {
    & reg.exe query $Key 2>&1 | Out-Null
    return ($LASTEXITCODE -eq 0)
}

function Get-DefenderServiceNames {
    $names = New-Object System.Collections.Generic.List[string]
    foreach ($name in $script:FixedServices) { $names.Add($name) }
    try {
        Get-ChildItem 'HKLM:\SYSTEM\CurrentControlSet\Services' -ErrorAction SilentlyContinue |
            Where-Object { $_.PSChildName -like 'webthreatdefusersvc_*' } |
            ForEach-Object { $names.Add($_.PSChildName) }
    } catch { }
    return ($names | Select-Object -Unique)
}

function Get-DefenderDirectories {
    $list = New-Object System.Collections.Generic.List[string]
    $list.Add((Join-Path $env:ProgramFiles 'Windows Defender'))
    $list.Add((Join-Path $env:ProgramFiles 'Windows Defender Advanced Threat Protection'))
    $list.Add((Join-Path $env:ProgramData 'Microsoft\Windows Defender'))
    $list.Add((Join-Path $env:ProgramData 'Microsoft\Windows Defender Advanced Threat Protection'))
    $list.Add((Join-Path $env:ProgramData 'Microsoft\Windows Security Health'))
    if (${env:ProgramFiles(x86)}) {
        $list.Add((Join-Path ${env:ProgramFiles(x86)} 'Windows Defender'))
    }
    $existing = @()
    foreach ($path in $list) {
        if ($path -and (Test-Path -LiteralPath $path)) { $existing += $path }
    }
    return ($existing | Select-Object -Unique)
}

function Get-RegistryTargets {
    return @(
        'HKLM\SOFTWARE\Microsoft\Windows Defender',
        'HKLM\SOFTWARE\Microsoft\Windows Defender Security Center',
        'HKLM\SOFTWARE\Microsoft\Windows Advanced Threat Protection',
        'HKLM\SOFTWARE\Microsoft\Windows Security Health',
        'HKLM\SOFTWARE\Policies\Microsoft\Windows Defender',
        'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Run',
        'HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run',
        'HKLM\SOFTWARE\Policies\Microsoft\Windows\System',
        'HKLM\SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter',
        'HKLM\SYSTEM\CurrentControlSet\Services\SharedAccess\Parameters\FirewallPolicy',
        'HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy',
        'HKCR\*\shellex\ContextMenuHandlers\EPP',
        'HKCR\Directory\shellex\ContextMenuHandlers\EPP',
        'HKCR\Drive\shellex\ContextMenuHandlers\EPP'
    )
}

function New-Journal {
    $mode = 'fast'
    if ($SafeMode) { $mode = 'safemode' }
    return [pscustomobject]@{
        version   = 1
        createdAt = (Get-Date).ToString('o')
        stage     = 'prepared'
        mode      = $mode
        exports   = @()
        files     = @()
        tasks     = @()
        exclusions = @()
        firewall  = $false
        heartbeat = (Get-Date).ToString('o')
    }
}

function Load-Journal {
    if (-not (Test-Path -LiteralPath $script:JournalPath)) { return $null }
    try {
        return (Get-Content -LiteralPath $script:JournalPath -Raw -Encoding UTF8 | ConvertFrom-Json)
    } catch {
        Write-Log ('journal read failed: ' + $_.Exception.Message)
        return $null
    }
}

function Save-Journal($Journal) {
    if (-not $Journal) { return }
    $Journal.heartbeat = (Get-Date).ToString('o')
    try {
        ($Journal | ConvertTo-Json -Depth 8) | Set-Content -LiteralPath $script:JournalPath -Encoding UTF8
    } catch {
        Write-Log ('journal write failed: ' + $_.Exception.Message)
    }
}

function Backup-Registry($Journal) {
    $targets = @()
    $targets += Get-RegistryTargets
    foreach ($service in Get-DefenderServiceNames) {
        $targets += ('HKLM\SYSTEM\CurrentControlSet\Services\' + $service)
    }

    $index = 0
    foreach ($key in $targets) {
        $index++
        $exists = Test-RegKey $key
        $file = Join-Path $script:RegDir ('{0:D3}.reg' -f $index)
        if ($exists) {
            Exec 'reg.exe' @('export', $key, $file, '/y') | Out-Null
            if (-not (Test-Path -LiteralPath $file)) { $exists = $false }
        }
        $Journal.exports += [pscustomobject]@{
            key     = $key
            file    = $file
            existed = $exists
        }
        Write-Log ('registry backup: ' + $key + ' exists=' + $exists)
    }
}

function Backup-Files($Journal) {
    foreach ($path in Get-DefenderDirectories) {
        $owner = ''
        $ownerSid = ''
        try {
            $acl = Get-Acl -LiteralPath $path
            $owner = $acl.Owner
            $ownerSid = (New-Object System.Security.Principal.NTAccount($owner)).Translate([System.Security.Principal.SecurityIdentifier]).Value
        } catch { }

        $Journal.files += [pscustomobject]@{
            path     = $path
            bak      = ($path + '.wotbo_bak')
            owner    = $owner
            ownerSid = $ownerSid
            renamed  = $false
        }
        Write-Log ('file backup: ' + $path + ' owner=' + $owner)
    }
}

function Backup-Tasks($Journal) {
    $index = 0
    foreach ($name in $script:TaskNames) {
        $index++
        $file = Join-Path $script:TaskDir ('{0:D3}.xml' -f $index)
        $exists = $false
        & schtasks.exe /query /tn $name /xml 2>$null | Out-File -LiteralPath $file -Encoding Unicode -Force
        if ($LASTEXITCODE -eq 0 -and (Test-Path -LiteralPath $file)) {
            $exists = $true
        } else {
            Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
        }
        $Journal.tasks += [pscustomobject]@{
            name    = $name
            file    = $file
            existed = $exists
            disabled = $false
        }
        Write-Log ('task backup: ' + $name + ' exists=' + $exists)
    }
}

function Add-DefenderExclusions($Journal) {
    $service = Get-Service -Name 'WinDefend' -ErrorAction SilentlyContinue
    if (-not $service -or $service.Status -ne 'Running') { return }
    $drive = $env:SystemDrive + '\'
    try {
        Add-MpPreference -ExclusionPath $drive -ErrorAction Stop
        $Journal.exclusions += [pscustomobject]@{ path = $drive }
        Save-Journal $Journal
        Write-Log ('exclusion added: ' + $drive)
    } catch {
        Write-Log ('exclusion failed: ' + $_.Exception.Message)
    }
}

function Stop-DefenderServices {
    $names = @('WdNisSvc', 'Sense', 'MDCoreSvc', 'MpDefenderCoreService', 'SecurityHealthService', 'WinDefend')
    foreach ($name in $names) {
        $service = Get-Service -Name $name -ErrorAction SilentlyContinue
        if (-not $service) { continue }
        if ($service.Status -eq 'Stopped') { continue }
        try {
            Stop-Service -Name $name -Force -ErrorAction Stop
            Write-Log ('stopped: ' + $name)
        } catch {
            Write-Log ('stop failed: ' + $name + ' :: ' + $_.Exception.Message)
        }
    }

    Start-Sleep -Milliseconds 500
    $winDefend = Get-Service -Name 'WinDefend' -ErrorAction SilentlyContinue
    if ($winDefend -and $winDefend.Status -ne 'Stopped') {
        Write-Log 'windefend is still running - tamper protection is active'
        return $false
    }
    return $true
}

function Protect-DefenderPath([string]$Path) {
    Write-Log ('taking ownership: ' + $Path)
    & takeown.exe /f $Path /r /d Y 2>&1 | ForEach-Object { Write-Log ('  ' + [string]$_) } | Out-Null
    & icacls.exe $Path /grant '*S-1-5-32-544:(OI)(CI)F' /t /c /q 2>&1 | ForEach-Object { Write-Log ('  ' + [string]$_) } | Out-Null
    try {
        $item = Get-Item -LiteralPath $Path -Force
        if ($item.Attributes -band [System.IO.FileAttributes]::ReadOnly) {
            $item.Attributes = ($item.Attributes -bxor [System.IO.FileAttributes]::ReadOnly)
        }
    } catch { }
}

function Rename-DefenderDirectories($Journal) {
    $ok = $true
    foreach ($entry in @($Journal.files)) {
        if (-not (Test-Path -LiteralPath $entry.path)) { continue }
        if (Test-Path -LiteralPath $entry.bak) {
            Write-Log ('already renamed: ' + $entry.path)
            continue
        }
        try {
            Move-Item -LiteralPath $entry.path -Destination $entry.bak -Force -ErrorAction Stop
            $entry.renamed = $true
            Write-Log ('renamed: ' + $entry.path)
        } catch {
            $ok = $false
            Write-Log ('rename failed: ' + $entry.path + ' :: ' + $_.Exception.Message)
        }
    }
    return $ok
}

function Disable-DefenderServices {
    $ok = $true
    foreach ($name in Get-DefenderServiceNames) {
        $key = 'HKLM:\SYSTEM\CurrentControlSet\Services\' + $name
        if (-not (Test-Path -LiteralPath $key)) { continue }
        try {
            Set-ItemProperty -LiteralPath $key -Name 'Start' -Value 4 -Type DWord -Force -ErrorAction Stop
            $protected = Get-ItemProperty -LiteralPath $key -Name 'LaunchProtected' -ErrorAction SilentlyContinue
            if ($protected) {
                Set-ItemProperty -LiteralPath $key -Name 'LaunchProtected' -Value 4 -Type DWord -Force -ErrorAction Stop
            }
            Write-Log ('disabled service: ' + $name)
        } catch {
            $ok = $false
            Write-Log ('service write failed: ' + $name + ' :: ' + $_.Exception.Message)
        }
    }
    return $ok
}

function Disable-DefenderTasks($Journal) {
    foreach ($entry in @($Journal.tasks)) {
        if (-not $entry.existed) { continue }
        $code = Exec 'schtasks.exe' @('/change', '/tn', $entry.name, '/disable')
        if ($code -eq 0) {
            $entry.disabled = $true
            Write-Log ('task disabled: ' + $entry.name)
        }
    }
}

function Set-RegValue([string]$Key, [string]$Name, [object]$Value, [string]$Kind) {
    try {
        if (-not (Test-Path -LiteralPath $Key)) {
            New-Item -Path $Key -Force -ErrorAction Stop | Out-Null
        }
        New-ItemProperty -LiteralPath $Key -Name $Name -Value $Value -PropertyType $Kind -Force -ErrorAction Stop | Out-Null
        Write-Log ('policy: ' + $Key + '\' + $Name + '=' + [string]$Value)
    } catch {
        Write-Log ('policy failed: ' + $Key + '\' + $Name + ' :: ' + $_.Exception.Message)
    }
}

function Apply-DefenderPolicies {
    $policy = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows Defender'
    Set-RegValue $policy 'DisableAntiSpyware' 1 'DWord'
    Set-RegValue $policy 'DisableAntiVirus' 1 'DWord'

    $realtime = $policy + '\Real-Time Protection'
    Set-RegValue $realtime 'DisableRealtimeMonitoring' 1 'DWord'
    Set-RegValue $realtime 'DisableBehaviorMonitoring' 1 'DWord'
    Set-RegValue $realtime 'DisableOnAccessProtection' 1 'DWord'
    Set-RegValue $realtime 'DisableScanOnRealtimeEnable' 1 'DWord'

    $spynet = $policy + '\Spynet'
    Set-RegValue $spynet 'SpynetReporting' 0 'DWord'
    Set-RegValue $spynet 'SubmitSamplesConsent' 2 'DWord'

    Set-RegValue 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\System' 'EnableSmartScreen' 0 'DWord'
    Set-RegValue 'HKLM:\SOFTWARE\Policies\Microsoft\MicrosoftEdge\PhishingFilter' 'EnabledV9' 0 'DWord'
    Set-RegValue 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer' 'SmartScreenEnabled' 'Off' 'String'
}

function Disable-Firewall($Journal) {
    Exec 'netsh.exe' @('advfirewall', 'set', 'allprofiles', 'state', 'off') | Out-Null
    $Journal.firewall = $true
    Save-Journal $Journal
    Write-Log 'firewall profiles disabled'
}

function Remove-EppHandlers {
    foreach ($key in @(
            'HKCR\*\shellex\ContextMenuHandlers\EPP',
            'HKCR\Directory\shellex\ContextMenuHandlers\EPP',
            'HKCR\Drive\shellex\ContextMenuHandlers\EPP')) {
        if (Test-RegKey $key) {
            Exec 'reg.exe' @('delete', $key, '/f') | Out-Null
            Write-Log ('epp handler removed: ' + $key)
        }
    }
}

function Remove-SecurityHealthRun {
    $run = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Run'
    try {
        if (Get-ItemProperty -LiteralPath $run -Name 'SecurityHealth' -ErrorAction SilentlyContinue) {
            Remove-ItemProperty -LiteralPath $run -Name 'SecurityHealth' -Force -ErrorAction Stop
            Write-Log 'security health run value removed'
        }
    } catch {
        Write-Log ('run value failed: ' + $_.Exception.Message)
    }
}

function Get-DefenderStatus {
    $status = [ordered]@{
        windefend = 'Absent'
        wdfilter  = 'Absent'
        mp        = 'unavailable'
        tamper    = ''
        antivirus = ''
        realtime  = ''
        amservice = ''
        processes = ''
    }

    $service = Get-Service -Name 'WinDefend' -ErrorAction SilentlyContinue
    if ($service) { $status.windefend = [string]$service.Status }

    $driver = Get-Service -Name 'WdFilter' -ErrorAction SilentlyContinue
    if ($driver) { $status.wdfilter = [string]$driver.Status }

    $processes = @(Get-Process -Name 'MsMpEng', 'MpDefenderCoreService', 'NisSrv', 'MsSense', 'SecurityHealthService' -ErrorAction SilentlyContinue |
        Select-Object -ExpandProperty Name -Unique)
    $status.processes = ($processes -join ',')

    try {
        $mp = Get-CimInstance -Namespace 'root\Microsoft\Windows\Defender' -ClassName 'MSFT_MpComputerStatus' -ErrorAction Stop
        $status.mp = 'ok'
        try { $status.tamper = [string]([int]$mp.IsTamperProtected) } catch { }
        try { $status.antivirus = [string]([int]$mp.AntivirusEnabled) } catch { }
        try { $status.realtime = [string]([int]$mp.RealTimeProtectionEnabled) } catch { }
        try { $status.amservice = [string]([int]$mp.AMServiceEnabled) } catch { }
    } catch {
        $status.mp = 'unavailable'
    }

    return $status
}

function Write-VerifyFile($Status) {
    $lines = @()
    foreach ($key in $Status.Keys) {
        $lines += ($key + '=' + [string]$Status[$key])
    }
    try { Set-Content -LiteralPath $script:VerifyPath -Value $lines -Encoding ASCII } catch { }
}

function Remove-SafeBoot {
    Exec 'bcdedit.exe' @('/deletevalue', '{current}', 'safeboot') | Out-Null
    Write-Log 'safeboot flag removed'
}

function Remove-StageTasks {
    foreach ($name in @($script:StageTask, $script:WatchdogTask)) {
        Exec 'schtasks.exe' @('/delete', '/tn', $name, '/f') | Out-Null
    }
}

function Remove-ArmedHelpers {
    try {
        Remove-ItemProperty -LiteralPath ('Registry::' + $script:RunOnceKey) -Name $script:RunOnceValue -Force -ErrorAction SilentlyContinue
        Write-Log 'runonce helper removed'
    } catch { }

    try {
        $winlogon = 'HKLM:\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Winlogon'
        $originalFile = Join-Path $script:Root 'userinit-original.txt'
        $loginRunner = Join-Path $script:Root 'wotbo-login.cmd'

        if (Test-Path -LiteralPath $originalFile) {
            $original = Get-Content -LiteralPath $originalFile -Raw
            Set-ItemProperty -LiteralPath $winlogon -Name 'Userinit' -Value $original -Type String -Force
            Remove-Item -LiteralPath $originalFile -Force -ErrorAction SilentlyContinue
            Write-Log 'userinit restored from backup'
        } else {
            $current = (Get-ItemProperty -LiteralPath $winlogon -Name 'Userinit' -ErrorAction Stop).Userinit
            if ($current -and $current.Contains(',' + $loginRunner)) {
                $cleaned = $current.Replace(',' + $loginRunner, '')
                Set-ItemProperty -LiteralPath $winlogon -Name 'Userinit' -Value $cleaned -Type String -Force
                Write-Log 'userinit hook removed'
            }
        }
    } catch {
        Write-Log ('userinit cleanup failed: ' + $_.Exception.Message)
    }

    try {
        Exec 'schtasks.exe' @('/delete', '/tn', '\WOTBO\DefenderFinalize', '/f') | Out-Null
        Exec 'sc.exe' @('delete', 'WotboDefender') | Out-Null
        Remove-Item -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Minimal\WotboDefender' -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Network\WotboDefender' -Recurse -Force -ErrorAction SilentlyContinue
        foreach ($name in 'service-phase.txt', 'WotboDefender.exe') {
            $file = Join-Path $script:Root $name
            if (Test-Path -LiteralPath $file) {
                Remove-Item -LiteralPath $file -Force -ErrorAction SilentlyContinue
            }
        }
        Write-Log 'service helper removed'
    } catch { }

    try {
        if (Test-Path -LiteralPath $script:Desktop) {
            Get-ChildItem -LiteralPath $script:Desktop -Filter 'WOTBO - *.cmd' -ErrorAction SilentlyContinue |
                ForEach-Object { Remove-Item -LiteralPath $_.FullName -Force -ErrorAction SilentlyContinue }
            Write-Log 'desktop helpers removed'
        }
    } catch { }
}

function Test-SafeModeBoot {
    # BCD-флаг safeboot стоит и в обычной загрузке (он же взведён на следующую),
    # поэтому смотрим признак текущей загрузки, который создаёт только safe mode.
    if (Test-Path -LiteralPath 'HKLM:\SYSTEM\CurrentControlSet\Control\SafeBoot\Option') {
        return $true
    }
    return $false
}

function Invoke-Prepare {
    $journal = New-Journal
    Backup-Registry $journal
    Backup-Files $journal
    Backup-Tasks $journal
    Save-Journal $journal
    Write-State 'prepared' 'ok' 'backup complete'
    Write-Log 'prepare complete'
}

function Invoke-Apply {
    $journal = Load-Journal
    if (-not $journal) {
        Write-Log 'no journal, preparing now'
        Invoke-Prepare
        $journal = Load-Journal
    }
    if (-not $journal) {
        Write-State 'failed' 'error' 'journal is not available'
        return
    }

    Write-Log ('apply stage=' + $journal.stage + ' safeMode=' + [string]$SafeMode)

    if ($SafeMode -and -not (Test-SafeModeBoot)) {
        Write-Log 'safe mode was requested but the current boot is normal, skipping'
        if ($journal.stage -ne 'applied' -and $journal.stage -ne 'restored') {
            Write-State 'blocked' 'blocked' 'not booted in safe mode'
        } else {
            Remove-StageTasks
            Remove-ArmedHelpers
        }
        return
    }

    if ($journal.stage -eq 'applied') {
        Write-Log 'already applied'
        if ($SafeMode) {
            Remove-SafeBoot
            Remove-StageTasks
            Remove-ArmedHelpers
            Write-State 'applied' 'ok' 'already applied'
        }
        return
    }

    Write-State 'applying' 'ok' 'started'
    Add-DefenderExclusions $journal

    if (-not $SafeMode) {
        if (-not (Stop-DefenderServices)) {
            Save-Journal $journal
            Write-State 'blocked' 'blocked' 'windefend is protected by tamper protection'
            return
        }
    }

    foreach ($path in @(Get-DefenderDirectories)) {
        Protect-DefenderPath $path
    }

    $renamed = Rename-DefenderDirectories $journal
    $services = Disable-DefenderServices
    Disable-DefenderTasks $journal
    Apply-DefenderPolicies
    Disable-Firewall $journal
    Remove-EppHandlers
    Remove-SecurityHealthRun

    Save-Journal $journal

    if (-not $renamed -or -not $services) {
        if (-not $SafeMode) {
            Write-State 'blocked' 'blocked' 'files or services are locked'
            return
        }
    }

    $journal.stage = 'applied'
    Save-Journal $journal
    Write-State 'applied' 'ok' 'defender disabled'
    Remove-ArmedHelpers

    if ($SafeMode) {
        Remove-SafeBoot
        Remove-StageTasks
        Write-Log 'rebooting to normal mode'
        Start-Sleep -Seconds 2
        Exec 'shutdown.exe' @('/r', '/t', '10', '/f', '/c', 'WOTBO: Defender disabled, rebooting') | Out-Null
    }
}

function Restore-DefenderDirectories($Journal) {
    foreach ($entry in @($Journal.files)) {
        if (-not (Test-Path -LiteralPath $entry.bak)) { continue }
        try {
            if (Test-Path -LiteralPath $entry.path) {
                Remove-Item -LiteralPath $entry.path -Recurse -Force -ErrorAction SilentlyContinue
            }
            Move-Item -LiteralPath $entry.bak -Destination $entry.path -Force -ErrorAction Stop
            Write-Log ('restored path: ' + $entry.path)
            if ($entry.ownerSid) {
                & icacls.exe $entry.path /setowner ('*' + $entry.ownerSid) /t /c /q 2>&1 | ForEach-Object { Write-Log ('  ' + [string]$_) } | Out-Null
            }
        } catch {
            Write-Log ('restore path failed: ' + $entry.path + ' :: ' + $_.Exception.Message)
        }
    }
}

function Import-Registry($Journal) {
    $exports = @($Journal.exports)
    for ($i = $exports.Count - 1; $i -ge 0; $i--) {
        $entry = $exports[$i]
        if ($entry.existed) {
            if (Test-Path -LiteralPath $entry.file) {
                # reg import только добавляет и перезаписывает, поэтому сначала
                # удаляем ветку целиком: иначе значения, записанные твиком,
                # останутся (например, политики Defender).
                Exec 'reg.exe' @('delete', $entry.key, '/f') | Out-Null
                Exec 'reg.exe' @('import', $entry.file) | Out-Null
            } else {
                Write-Log ('export missing: ' + $entry.file)
            }
        } else {
            Exec 'reg.exe' @('delete', $entry.key, '/f') | Out-Null
        }
    }
}

function Enable-DefenderTasks($Journal) {
    foreach ($entry in @($Journal.tasks)) {
        if (-not $entry.existed) { continue }
        Exec 'schtasks.exe' @('/change', '/tn', $entry.name, '/enable') | Out-Null
    }
}

function Enable-Firewall($Journal) {
    if (-not $Journal.firewall) { return }
    Exec 'netsh.exe' @('advfirewall', 'set', 'allprofiles', 'state', 'on') | Out-Null
    Write-Log 'firewall profiles enabled'
}

function Remove-DefenderExclusions($Journal) {
    foreach ($entry in @($Journal.exclusions)) {
        try {
            Remove-MpPreference -ExclusionPath $entry.path -ErrorAction Stop
            Write-Log ('exclusion removed: ' + $entry.path)
        } catch { }
    }
}

function Start-DefenderServices {
    foreach ($name in @('WinDefend', 'WdNisSvc', 'SecurityHealthService')) {
        $service = Get-Service -Name $name -ErrorAction SilentlyContinue
        if (-not $service) { continue }
        try {
            Start-Service -Name $name -ErrorAction Stop
            Write-Log ('started: ' + $name)
        } catch {
            Write-Log ('start failed: ' + $name + ' :: ' + $_.Exception.Message)
        }
    }
}

function Invoke-Restore {
    $journal = Load-Journal
    if (-not $journal) {
        Write-State 'failed' 'error' 'no journal to restore'
        return
    }

    if ($SafeMode -and -not (Test-SafeModeBoot)) {
        Write-Log 'safe mode was requested but the current boot is normal, skipping'
        if ($journal.stage -ne 'applied' -and $journal.stage -ne 'restored') {
            Write-State 'blocked' 'blocked' 'not booted in safe mode'
        } else {
            Remove-StageTasks
            Remove-ArmedHelpers
        }
        return
    }

    Write-Log ('restore stage=' + $journal.stage + ' safeMode=' + [string]$SafeMode)

    if ($journal.stage -eq 'restored') {
        Write-Log 'already restored'
        if ($SafeMode) {
            Remove-SafeBoot
            Remove-StageTasks
            Remove-ArmedHelpers
            Write-State 'restored' 'ok' 'already restored'
        }
        return
    }

    Write-State 'restoring' 'ok' 'started'
    Restore-DefenderDirectories $journal
    Import-Registry $journal
    Enable-DefenderTasks $journal
    Enable-Firewall $journal
    Remove-DefenderExclusions $journal
    Start-DefenderServices

    try {
        Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue
        Set-MpPreference -DisableBehaviorMonitoring $false -ErrorAction SilentlyContinue
        Set-MpPreference -DisableOnAccessProtection $false -ErrorAction SilentlyContinue
        Write-Log 'defender preferences re-enabled'
    } catch {
        Write-Log ('preference restore failed: ' + $_.Exception.Message)
    }

    $journal.stage = 'restored'
    Save-Journal $journal
    Write-State 'restored' 'ok' 'defender restored'
    Remove-ArmedHelpers

    if ($SafeMode) {
        Remove-SafeBoot
        Remove-StageTasks
        $finalize = Join-Path $script:Root 'wotbo-finalize.cmd'
        if (Test-Path -LiteralPath $finalize) {
            Exec 'schtasks.exe' @('/create', '/tn', '\WOTBO\DefenderFinalize', '/tr', $finalize, '/sc', 'onstart', '/delay', '0001:00', '/ru', 'SYSTEM', '/f') | Out-Null
            Write-Log 'finalize task scheduled'
        }
        Start-Sleep -Seconds 2
        Exec 'shutdown.exe' @('/r', '/t', '10', '/f', '/c', 'WOTBO: Defender restored, rebooting') | Out-Null
    }
}

function Invoke-Finalize {
    Write-Log 'finalize: waiting for windefend'
    $deadline = (Get-Date).AddMinutes(3)
    while ((Get-Date) -lt $deadline) {
        $service = Get-Service -Name 'WinDefend' -ErrorAction SilentlyContinue
        if ($service -and $service.Status -eq 'Running') {
            break
        }
        Start-Sleep -Seconds 5
    }

    try {
        Set-MpPreference -DisableRealtimeMonitoring $false -ErrorAction SilentlyContinue
        Set-MpPreference -DisableBehaviorMonitoring $false -ErrorAction SilentlyContinue
        Set-MpPreference -DisableOnAccessProtection $false -ErrorAction SilentlyContinue
        Set-MpPreference -DisableScanOnRealtimeEnable $false -ErrorAction SilentlyContinue
        Write-Log 'finalize: preferences applied'
    } catch {
        Write-Log ('finalize preference failed: ' + $_.Exception.Message)
    }

    Start-Sleep -Seconds 10
    Invoke-Verify

    Exec 'schtasks.exe' @('/delete', '/tn', '\WOTBO\DefenderFinalize', '/f') | Out-Null
    Remove-Item -LiteralPath (Join-Path $script:Root 'wotbo-finalize.cmd') -Force -ErrorAction SilentlyContinue
    Write-Log 'finalize: done'
}

function Invoke-Verify {
    $status = Get-DefenderStatus
    Write-VerifyFile $status
    foreach ($key in $status.Keys) {
        Write-Log ('verify: ' + $key + '=' + [string]$status[$key])
    }
}

function Invoke-Watchdog {
    if (-not (Test-SafeModeBoot)) {
        Write-Log 'not a safe mode boot, watchdog does nothing'
        return
    }

    $journal = Load-Journal
    $stage = ''
    if ($journal) { $stage = [string]$journal.stage }

    Write-Log ('watchdog: stage=' + $stage + ' safe mode boot')
    Remove-SafeBoot
    Remove-StageTasks
    if ($stage -ne 'applied' -and $stage -ne 'restored') {
        Write-State 'failed' 'error' 'safe mode stage did not finish'
        Exec 'shutdown.exe' @('/r', '/t', '10', '/f', '/c', 'WOTBO: safe mode stage failed, rebooting') | Out-Null
    }
}

Write-Log ('phase=' + $Phase + ' safeMode=' + [string]$SafeMode + ' root=' + $Root)

$script:EngineMutex = $null
try {
    $script:EngineMutex = New-Object System.Threading.Mutex($false, 'Global\WOTBO-DefenderEngine')
} catch {
    try {
        $script:EngineMutex = New-Object System.Threading.Mutex($false, 'Local\WOTBO-DefenderEngine')
    } catch {
        $script:EngineMutex = $null
    }
}

function Enter-Engine {
    if (-not $script:EngineMutex) {
        return $true
    }

    try {
        if ($script:EngineMutex.WaitOne(0)) {
            return $true
        }
    } catch {
    }
    Write-Log 'another engine instance is running, skip'
    return $false
}

function Exit-Engine {
    if (-not $script:EngineMutex) { return }
    try { $script:EngineMutex.ReleaseMutex() } catch { }
}

try {
    switch ($Phase) {
        'prepare' { Invoke-Prepare }
        'apply' {
            if (Enter-Engine) {
                try { Invoke-Apply } finally { Exit-Engine }
            }
        }
        'restore' {
            if (Enter-Engine) {
                try { Invoke-Restore } finally { Exit-Engine }
            }
        }
        'verify' { Invoke-Verify }
        'watchdog' {
            if (Enter-Engine) {
                try { Invoke-Watchdog } finally { Exit-Engine }
            }
        }
        'finalize' {
            if (Enter-Engine) {
                try { Invoke-Finalize } finally { Exit-Engine }
            }
        }
    }
} catch {
    Write-Log ('fatal: ' + $_.Exception.Message)
    Write-State 'failed' 'error' $_.Exception.Message

    if ($SafeMode) {
        Remove-SafeBoot
        Remove-StageTasks
        Remove-ArmedHelpers
        Exec 'shutdown.exe' @('/r', '/t', '10', '/f', '/c', 'WOTBO: failed, rebooting to normal mode') | Out-Null
    }
}
