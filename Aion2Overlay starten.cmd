@echo off
setlocal
set "overlayScript=%~dp0scripts\Start-Overlay.ps1"
if not exist "%overlayScript%" set "overlayScript=%~dp0..\..\scripts\Start-Overlay.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%overlayScript%" -ShowError
exit /b %errorlevel%
