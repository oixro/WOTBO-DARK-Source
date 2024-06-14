set "TI=%windir%\oixro\su.exe /wrs cmd.exe /c "
taskkill /f /im explorer.exe >nul 2>&1

%TI% del /q "%windir%\SystemResources\imageres.dll.mun"

timeout /t 2 /nobreak >nul

%TI% ren "%windir%\SystemResources\imageres.dll.mun_bak" imageres.dll.mun

del /q /f /a:h "%windir%\windows.ico" >nul
del /q /f /a:h "%windir%\desktop.ini" >nul
del /q /f /a:h "%ProgramFiles%\x64.ico" >nul
del /q /f /a:h "%ProgramFiles%\desktop.ini" >nul
del /q /f /a:h "%ProgramFiles(x86)%\x86.ico" >nul
del /q /f /a:h "%ProgramFiles(x86)%\desktop.ini" >nul
del /q /f /a:h "%SystemDrive%\Users\users.ico" >nul
del /q /f /a:h "%SystemDrive%\Users\desktop.ini" >nul

reg add "HKCR\CompressedFolder\DefaultIcon" /v "" /t REG_EXPAND_SZ /d "%windir%\system32\zipfldr.dll" /f >nul

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
