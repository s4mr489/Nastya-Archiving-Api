@echo off
echo Running Python dependency installer with administrator privileges...

:: Check if running as administrator
net session >nul 2>&1
if %errorLevel% == 0 (
    echo Running with administrator privileges.
) else (
    echo Requesting administrator privileges...
    powershell -Command "Start-Process cmd -ArgumentList '/c cd /d %~dp0 && %~dpnx0' -Verb RunAs"
    exit /b
)

:: Run the PowerShell script
powershell.exe -ExecutionPolicy Bypass -File "%~dp0install_dependencies_direct.ps1"
echo.
echo Installation completed. Press any key to exit...
pause > nul