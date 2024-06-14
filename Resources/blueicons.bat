set "TI=%windir%\oixro\su.exe /wrs cmd.exe /c "
taskkill /f /im explorer.exe >nul 2>&1

del "%APPDATA%\Microsoft\Windows\Start Menu\Programs\System Tools\File Explorer.lnk" >nul 2>&1
del "%APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar\File Explorer.lnk" >nul 2>&1

%TI% ren "%windir%\SystemResources\imageres.dll.mun" imageres.dll.mun_bak

timeout /t 1 /nobreak >nul

%TI% copy "%windir%\oixro\BlueIcon Minimal\imageres.dll.mun" "%windir%\SystemResources"

reg add "HKCR\CompressedFolder\DefaultIcon" /v "" /t REG_EXPAND_SZ /d "%windir%\system32\imageres.dll,165" /f >nul

xcopy "%windir%\oixro\BlueIcon Minimal\File Explorer.lnk" "%APPDATA%\Microsoft\Windows\Start Menu\Programs\System Tools" /y >nul
xcopy "%windir%\oixro\BlueIcon Minimal\File Explorer.lnk" "%APPDATA%\Microsoft\Internet Explorer\Quick Launch\User Pinned\TaskBar" /y >nul
%TI% xcopy "%windir%\oixro\BlueIcon Minimal\x64" "%ProgramFiles%" /y >nul
%TI% xcopy "%windir%\oixro\BlueIcon Minimal\x86" "%ProgramFiles(x86)%" /y >nul
%TI% xcopy "%windir%\oixro\BlueIcon Minimal\users" "%SystemDrive%\Users" /y >nul
%TI% xcopy "%windir%\oixro\BlueIcon Minimal\windows" "%windir%" /y >nul

cd /d "%ProgramFiles%" & attrib +h x64.ico >nul & cd /d "%ProgramFiles%" & attrib +h desktop.ini >nul
cd /d "%ProgramFiles(x86)%" & attrib +h x86.ico >nul & cd /d "%ProgramFiles(x86)%" & attrib +h desktop.ini >nul
cd /d "%SystemDrive%\Users" & attrib +h users.ico >nul & cd /d "%SystemDrive%\Users" & attrib +h desktop.ini >nul
cd /d "%windir%" & attrib +h windows.ico >nul & cd /d "%windir%" & attrib +h desktop.ini >nul

ATTRIB +R "%ProgramFiles(x86)%" >nul
ATTRIB +R "%ProgramFiles%" >nul
ATTRIB +R "%SystemDrive%\Users" >nul
ATTRIB +R "%windir%" >nul

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
