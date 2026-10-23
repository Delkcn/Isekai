@echo off
setlocal
cd /d "%~dp0"
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%~dp0Build.ps1" -Target App
if errorlevel 1 (
 pause
 exit /b 1
)
start "" "%~dp0IsekaijouchoPet.exe"
