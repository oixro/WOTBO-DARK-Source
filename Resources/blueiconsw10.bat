set "TI=%windir%\oixro\nircmd.exe execmd"
taskkill /f /im explorer.exe >nul 2>&1

%TI% %windir%\oixro\TrInstaller.exe /c %TI% ren "%windir%\System32\imageres.dll" "imageres.dll_bak"

timeout /t 2 /nobreak >nul

%TI% %windir%\oixro\TrInstaller.exe /c %TI% copy "%windir%\oixro\imageres.dll" "%windir%\System32"

pushd "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /a:s IconCache* >nul 2>&1
del /f /a:s thumbcache* >nul 2>&1
del /f IconCache* >nul 2>&1
del /f thumbcache* >nul 2>&1
popd
pushd "%userprofile%\AppData\Local"
del /f /a:s IconCache* >nul 2>&1
del /f /a:s thumbcache* >nul 2>&1
del /f IconCache* >nul 2>&1
del /f thumbcache* >nul 2>&1
popd
timeout /t 1 /nobreak >nul
start explorer