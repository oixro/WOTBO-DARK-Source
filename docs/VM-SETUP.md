# P0 — рабочее окружение: macOS + Windows 11 VM

Цель этапа: писать код в VSCode на Mac, собирать и запускать `WOTBO.exe` в
Windows 11 VM, получать логи обратно на Mac. Логику приложения на этом этапе
не меняем.

> **Важно про архитектуру.** Mac у тебя на Apple Silicon (`arm64`), Parallels
> установлен. Значит VM будет с **Windows 11 ARM**, а WOTBO собирается под
> **x64** и запускается через встроенную эмуляцию x64. Сборка .NET Framework
> x64 там работает, но приложение и системные утилиты идут через эмуляцию.
> Если начнутся странности с DISM/реестром — понадобится x64-хост (Intel Mac
> или облачная x64 VM). Это не блокер для P0, но держи в голове.

---

## 0. Быстрый чек-лист (что делать по порядку)

```text
1. Mac:     создать ключ SSH                       (раздел 3)
2. Parallels: создать VM с Windows 11              (раздел 1)
3. VM:      учётка-администратор, обновления, сон off, снапшот P0-clean
4. VM:      скопировать проект в C:\src\WOTBO       (раздел 2)
5. VM:      elevated PowerShell -> setup-vm.ps1     (раздел 4)
6. VM:      doctor.ps1 -> все [ OK ]
7. Mac:     ssh по ключу, sync.sh, remote-build.sh  (разделы 5-6)
8. VM:      run.ps1 на рабочем столе, лог -> Mac    (раздел 7)
```

Каждый шаг проверяемый: если следующий не работает, причина почти всегда в
предыдущем.

---

## 1. Создание VM в Parallels

1. Открой Parallels Desktop → **New** → **Get Windows 11 from Microsoft**.
   Если у тебя есть ISO/образ 26H2 — выбери **Install Windows or another OS
   from a DVD or image file**.
2. Параметры VM (Configuration → General/Hardware):
   - 4 CPU (минимум 2);
   - 8 ГБ RAM (минимум 4);
   - 64–80 ГБ диск;
   - сеть — **Shared Network** (чтобы Mac видел VM по IP);
   - **Shared Clipboard / Shared Folders** можно выключить после настройки.
3. Установи Windows 11, создай локального пользователя, например `wotbo`
   (без пробелов и кириллицы в имени — оно попадёт в SSH-команды и пути).
4. Убедись, что пользователь — администратор:
   `Settings → Accounts → Other users → Account type = Administrator`.
5. Установи Parallels Tools (обычно предлагается автоматически).
6. Отключи сон: `Settings → System → Power` → Screen/sleep = Never.
7. Отключи быстрый запуск (Fast Startup): это частая причина странного
   состояния реестра после «выключения» VM.

```powershell
powercfg.exe /hibernate off
```

8. Узнай IP VM:

```powershell
ipconfig | Select-String IPv4
```

На Mac проверь:

```bash
ping <vm-ip>
```

IP лучше зафиксировать DHCP-reservation в Parallels, иначе после ребута
придётся заново экспортировать `WOTBO_VM_HOST`.

9. Поставь все обновления Windows и сделай снапшот **`P0-clean`**
   (Actions → Take Snapshot). Дальше любой эксперимент с твиками — revert сюда.

> **Про 26H2.** Если сборка свежая, `DISM`, `bcdedit` и `powercfg` могут вести
> себя иначе, чем на 24H2/25H2. Это одна из причин, зачем нужен снапшот: любые
> изменения системы откатываются одним кликом.


---

## 2. Первый доступ к репозиторию из VM

SSH ещё не настроен, поэтому используй один из вариантов:

- Parallels Shared Folders: подключи папку `~/Documents/wotbo` и скопируй
  `WOTBO-DARK-Source` в `C:\src\WOTBO`;
- drag-and-drop файлов из Mac в VM;
- скачай zip репозитория из git-хостинга прямо в VM.

Дальше все обновления будут идти через `build/sync.sh` по SSH.

> **Важно:** путь обязательно `C:\src\WOTBO` — без пробелов. `sync.sh` передаёт
> команды в `cmd.exe` (стандартный shell OpenSSH на Windows), и вложенные
> кавычки с пробелами там ломаются.

---

## 3. Ключ SSH на Mac

```bash
ssh-keygen -t ed25519 -C "wotbo-mac" -f ~/.ssh/wotbo_vm
cat ~/.ssh/wotbo_vm.pub
```

Скопируй вывод `cat` — он понадобится в следующем шаге.

Добавь в `~/.ssh/config`:

```sshconfig
Host wotbo-vm
    HostName 192.168.1.50
    User wotbo
    IdentityFile ~/.ssh/wotbo_vm
    IdentitiesOnly yes
```

Замени IP на адрес своей VM. Дальше вместо `wotbo@<vm-ip>` можно писать
`wotbo-vm`.

---

## 4. Первичная настройка Windows

В VM открой **PowerShell от имени администратора** и перейди в папку проекта:

```powershell
cd C:\src\WOTBO
Set-ExecutionPolicy Bypass -Scope Process -Force
.\build\setup-vm.ps1 -SshPublicKey "ssh-ed25519 AAAA... wotbo-mac"
```

Скрипт:

- включает длинные пути;
- создаёт `C:\src\WOTBO`;
- добавляет исключение Defender для рабочей папки;
- ставит и запускает OpenSSH Server;
- прописывает твой публичный ключ (для администратора — в
  `C:\ProgramData\ssh\administrators_authorized_keys` с правильным ACL);
- ставит VS 2022 Build Tools: workload **Managed Desktop Build Tools** +
  **.NET Framework 4.8 targeting pack** (Windows 11 SDK — опционально,
  при ошибке скрипт повторяет установку без него);
- выводит путь к MSBuild.

После установки Build Tools **закрой и снова открой PowerShell** — PATH и
`vswhere` обновляются только в новой сессии.

Проверка:

```powershell
.\build\doctor.ps1
```

Ожидаемый результат — все строки `[ OK ]`. Если есть `[FAIL]`, там же написано,
что именно не так.

### Если `Add-WindowsCapability` падает с `0x80240438`

Это значит, что Windows Update не отдаёт компонент: источник отключён
политикой, стоит WSUS или деблоат-скрипт снял `UseWindowsUpdateForOptionalComponents`.
Скрипт теперь сам пробует три пути (capability → включить источник компонентов
и повторить → MSI с GitHub), но если нужно руками:

```powershell
# 1. Разрешить Windows Update как источник компонентов
$servicing = 'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Policies\Servicing'
New-Item -Path $servicing -Force | Out-Null
Set-ItemProperty $servicing -Name UseWindowsUpdateForOptionalComponents -Value 1 -Type DWord
$au = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
New-Item -Path $au -Force | Out-Null
Set-ItemProperty $au -Name UseWUServer -Value 0 -Type DWord
Restart-Service wuauserv -Force
Add-WindowsCapability -Online -Name OpenSSH.Server~~~~0.0.1.0

# 2. Если не помогло — Win32-OpenSSH MSI с GitHub (для ARM64 — ARM64-сборка)
$rel = Invoke-RestMethod 'https://api.github.com/repos/PowerShell/Win32-OpenSSH/releases?per_page=5'
$arch = if ($env:PROCESSOR_ARCHITECTURE -eq 'ARM64') { 'ARM64' } else { 'Win64' }
$asset = $rel.assets | Where-Object { $_.name -match "^OpenSSH-$arch-v.*\.msi$" } | Select-Object -First 1
$msi = "$env:TEMP\$($asset.name)"
Invoke-WebRequest $asset.browser_download_url -OutFile $msi
Start-Process msiexec.exe -ArgumentList "/i `"$msi`" /qn /norestart" -Wait
Start-Service sshd
New-NetFirewallRule -Name OpenSSH-Server-In-TCP -DisplayName 'OpenSSH Server (sshd)' `
    -Enabled True -Direction Inbound -Protocol TCP -Action Allow -LocalPort 22
```

Если не работает ни то, ни другое — проверь, есть ли у VM интернет:
`Test-NetConnection github.com -Port 443`.

Если установка Build Tools через скрипт не удалась, поставь вручную через
**Visual Studio Installer → Build Tools 2022 → Modify**:

- workload **Managed Desktop Build Tools**;
- компонент **.NET Framework 4.8 targeting pack**;
- компонент **Windows 11 SDK** (не обязателен для WOTBO).

### Если MSBuild оказался в `C:\Program\...`

Признак в `doctor.ps1`:

```text
[ OK ] MSBuild C:\Program\MSBuild\Current\Bin\MSBuild.exe
```

Причина: установщик получил `--installPath C:\Program`, потому что путь
`C:\Program Files (x86)\...` развалился на аргументы. В `setup-vm.ps1` это
исправлено (установщик вызывается напрямую, PowerShell сам экранирует путь).

Если хочется привести установку в порядок:

1. **Параметры → Приложения → Установленные приложения** →
   **Visual Studio Build Tools 2022** → **Удалить**.
2. Проверить остатки:

```powershell
Get-ChildItem C:\Program -ErrorAction SilentlyContinue
```

3. Если там только `MSBuild` и мусор от установки — удалить папку:

```powershell
Remove-Item C:\Program -Recurse -Force
```

4. Повторить установку: `.\build\setup-vm.ps1 -SshPublicKey "..."`.
5. `.\build\doctor.ps1` — путь должен содержать
   `Microsoft Visual Studio\2022\BuildTools`.

Переустанавливать не обязательно: `build.ps1` находит MSBuild через `vswhere`,
так что сборка работает и из `C:\Program\...`. Это косметика, но лучше убрать,
пока на путь ничего не завязано.

---

## 5. Проверка SSH с Mac

Сначала ключ должен быть установлен. Отдельный скрипт (идемпотентный):

```powershell
# в VM, elevated PowerShell
.\build\install-ssh-key.ps1 -SshPublicKey "ssh-ed25519 AAAA... wotbo-mac"
```

Он пишет ключ в `C:\ProgramData\ssh\administrators_authorized_keys` с ACL
только `Administrators` + `SYSTEM`, дублирует в `%USERPROFILE%\.ssh\authorized_keys`,
перезапускает `sshd` и печатает IP для проверки.

```bash
ssh wotbo@<vm-ip> "powershell -NoProfile -Command \"$PSVersionTable.PSVersion\""
```

Если просит пароль или отказывает — смотреть, что именно предлагается:

```bash
ssh -v wotbo@<vm-ip> 2>&1 | grep -Ei 'authentications that can continue|Offering|denied'
```

В VM проверить:

```powershell
Get-Content C:\ProgramData\ssh\administrators_authorized_keys
icacls C:\ProgramData\ssh\administrators_authorized_keys
```

ACL должен быть только у `Administrators` и `SYSTEM`, без наследования.
Если SSH не отвечает:

```powershell
Get-Service sshd
Get-NetFirewallRule -Name OpenSSH-Server-In-TCP
```

---

## 6. Sync и сборка с Mac

```bash
cd /Users/oixro/Documents/wotbo/WOTBO-DARK-Source

# Вариант А: alias из ~/.ssh/config (рекомендуется)
export WOTBO_VM_SSH=wotbo-vm

# Вариант Б: адрес и пользователь
# export WOTBO_VM_HOST=10.211.55.6
# export WOTBO_VM_USER=wotbo

./build/sync.sh          # копирует исходники в C:\src\WOTBO
./build/remote-build.sh  # sync + сборка в VM + копирование логов и бинарника
```

Результат в VM:

```text
C:\src\WOTBO\artifacts\x64\Release\WOTBO.exe
C:\src\WOTBO\artifacts\logs\build-Release-*.log
```

На Mac появятся `artifacts/logs/` и `artifacts/x64/Release/WOTBO.exe`
(обе папки в `.gitignore`).

Проверить, что бинарник действительно x64, можно так:

```powershell
$path = "C:\src\WOTBO\artifacts\x64\Release\WOTBO.exe"
$bytes = [IO.File]::ReadAllBytes($path)
$pe = [BitConverter]::ToInt32($bytes, 0x3C)
"Machine: 0x{0:X4}" -f [BitConverter]::ToUInt16($bytes, $pe + 4)
```

Ожидается `0x8664`.

---

## 7. Запуск и логи

Запускать нужно в VM, на её рабочем столе — из-за UAC:

```powershell
cd C:\src\WOTBO
powershell -ExecutionPolicy Bypass -File .\build\run.ps1 -Configuration Release
```

`run.ps1`:

- запускает `WOTBO.exe` с повышением прав;
- ждёт завершения;
- копирует `%TEMP%\wotbo.log` в `artifacts\logs\wotbo-*.log`.

Лог на Mac можно забрать так:

```bash
scp -r wotbo@<vm-ip>:/C:/src/WOTBO/artifacts/logs/. ./artifacts/logs/
```

Если `scp` с windows-путём не сработает, используй Parallels Shared Folder
или `ssh` + `Get-Content`.

---

## 8. Снапшоты и тесты

Перед каждым прогоном, который меняет реестр/систему:

1. revert VM к `P0-clean` или к снапшоту нужной сборки Windows;
2. собери свежий exe;
3. запусти сценарий;
4. забери лог;
5. сделай новый снапшот, если проверяешь конкретную ветку.

Снапшоты, которые понадобятся дальше:

- `P0-clean` — чистая Windows 11 26H2 с Build Tools и SSH;
- `P1-services` — после инфраструктурного этапа;
- `P4-tweaks` — перед проверкой apply/rollback;
- `win10-22h2`, `win11-23h2`, `win11-24h2` — отдельные VM/снапшоты, когда
  дойдём до матрицы совместимости.

---

## 9. Ограничения Windows 11 ARM

- WOTBO x64 работает через эмуляцию. Это медленнее и может отличаться от
  нативного x64, особенно для `DISM`, `bcdedit`, `powercfg`.
- `vs_buildtools.exe` запускается как x64 под эмуляцией, MSBuild тоже.
  Компиляция IL в PE с `PlatformTarget=x64` от этого не зависит — на выходе
  всё равно `0x8664`.
- Windows 11 SDK для этой сборки не нужен: в проекте нет C++ и WinRT.
  `setup-vm.ps1` ставит его как опцию и повторяет установку без SDK, если
  компонент недоступен для ARM.
- Если появятся проблемы, которые не воспроизводятся на ARM:
  - используй x64-хост (Intel Mac / Parallels на Intel);
  - или облачную x64 Windows VM;
  - или отдельную x64 VM на гипервизоре с вложенной виртуализацией.

---

## 10. Troubleshooting

| Симптом | Что проверить |
|---|---|
| `Add-WindowsCapability` → `0x80240438` | Windows Update не источник компонентов: см. раздел 4 или MSI-фолбэк |
| `ssh: connection refused` | `Get-Service sshd`, firewall rule, IP VM |
| `Permission denied (publickey)` | ACL `administrators_authorized_keys`, ключ в файле, `sshd_config` |
| `MSBuild not found` | Build Tools workload, `vswhere`, закрыть/открыть PowerShell |
| `tar: not found` на Windows | Windows 11 обычно содержит `C:\Windows\System32\tar.exe`; иначе используй `scp -r` или shared folder |
| `tar: -C: not found` / странные ошибки `sync.sh` | путь с пробелами или изменённый `DefaultShell`; верни `C:\src\WOTBO` и `cmd.exe` |
| `ssh` просит пароль, хотя ключ добавлен | `administrators_authorized_keys` должен иметь ACL только `Administrators` + `SYSTEM` |
| Defender блокирует сборку/exe | исключения для `C:\src\WOTBO` и `artifacts` |
| UAC не даёт запустить | запускай `run.ps1` из VM, а не через SSH |
| Приложение не видит реестр | на этом этапе ожидаемо: x64-процесс и текущие пути ещё не переведены на `RegistryView` |
| Лог не копируется | проверь `%TEMP%` пользователя, от которого запущен exe |
| `PE machine: 0x014C` в build.ps1 | собралось под x86: проверь `Platform=x64` и что в `.sln` нет `Any CPU` |

---

## 11. Чек-лист P0

- [ ] VM Windows 11 26H2 создана, обновлена, снапшот `P0-clean` есть.
- [ ] OpenSSH работает, вход по ключу с Mac без пароля.
- [ ] `setup-vm.ps1` выполнен, MSBuild найден.
- [ ] `./build/remote-build.sh` собирает `artifacts\x64\Release\WOTBO.exe`.
- [ ] `Machine: 0x8664` подтверждён.
- [ ] `run.ps1` запускает приложение от администратора.
- [ ] `%TEMP%\wotbo.log` появляется и копируется в `artifacts\logs`.
- [ ] UI открывается как раньше, скриншоты панелей сохранены как baseline.
- [ ] `git status` на Mac не содержит случайных артефактов сборки.
