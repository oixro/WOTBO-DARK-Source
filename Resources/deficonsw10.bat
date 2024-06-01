set "TI=%windir%\oixro\nircmd.exe execmd"
taskkill /f /im explorer.exe >nul 2>&1

%TI% %windir%\oixro\TrInstaller.exe /c %TI% del /q "%windir%\System32\imageres.dll"
timeout /t 2 /nobreak >nul
%TI% %windir%\oixro\TrInstaller.exe /c %TI% ren "%windir%\System32\imageres.dll_bak" imageres.dll

pushd "%userprofile%\AppData\Local\Microsoft\Windows\Explorer"
del /f /a:s IconCache* >nul 2>&1
del /f /a:s thumbcache* >nul 2>&1
del /f IconCache* >nul 2>&1
del /f thumbcache* >nul 2>&1
popd
timeout /t 1 /nobreak >nul
pushd "%userprofile%\AppData\Local"
del /f /a:s IconCache* >nul 2>&1
del /f /a:s thumbcache* >nul 2>&1
del /f IconCache* >nul 2>&1
del /f thumbcache* >nul 2>&1
popd
timeout /t 1 /nobreak >nul
start explorer