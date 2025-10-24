@echo off
echo Starting AutoFixer Demo...
echo.

REM Check if PowerShell is available
powershell -Command "Get-Host" >nul 2>&1
if errorlevel 1 (
    echo PowerShell is not available. Please install PowerShell.
    pause
    exit /b 1
)

REM Run the demo script
powershell -ExecutionPolicy Bypass -File "demo-script.ps1" %*

echo.
echo Demo completed. Press any key to exit...
pause >nul