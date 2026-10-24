@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\Build.ps1" -Target AnimationCheck
if errorlevel 1 (
 pause
 exit /b 1
)
AnimationCheck.exe
set "PET_CHECK_EXIT=%ERRORLEVEL%"
pause
exit /b %PET_CHECK_EXIT%
