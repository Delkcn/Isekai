@echo off
setlocal
cd /d "%~dp0.."
powershell.exe -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%CD%\Build.ps1" -Target Tests
if errorlevel 1 (
 pause
 exit /b 1
)
SelfTest.exe
set "PET_TEST_EXIT=%ERRORLEVEL%"
pause
exit /b %PET_TEST_EXIT%
