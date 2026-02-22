@echo off
setlocal

set SERVICE_NAME=TaskWorkflow Scheduler
set EXE_PATH=%~dp0bin\Release\net10.0\win-x64\publish\TaskWorkflow.Scheduler.exe

:: Check for administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: This script must be run as Administrator.
    pause
    exit /b 1
)

echo -----------------------------------------------
echo  Installing: %SERVICE_NAME%
echo  Binary:     %EXE_PATH%
echo -----------------------------------------------

:: Stop and remove existing service if present
sc query "%SERVICE_NAME%" >nul 2>&1
if %errorlevel% equ 0 (
    echo Stopping existing service...
    sc stop "%SERVICE_NAME%" >nul 2>&1
    timeout /t 3 /nobreak >nul
    echo Removing existing service...
    sc delete "%SERVICE_NAME%"
    timeout /t 2 /nobreak >nul
)

:: Verify the executable exists
if not exist "%EXE_PATH%" (
    echo ERROR: Executable not found at %EXE_PATH%
    echo Please run 'dotnet publish -c Release' first.
    pause
    exit /b 1
)

:: Create the service
echo Creating service...
sc create "%SERVICE_NAME%" binPath= "%EXE_PATH%" start= auto
if %errorlevel% neq 0 (
    echo ERROR: Failed to create service.
    pause
    exit /b 1
)

:: Set description
sc description "%SERVICE_NAME%" "TaskWorkflow Scheduler - manages and triggers scheduled task workflows"

:: Start the service
echo Starting service...
sc start "%SERVICE_NAME%"
if %errorlevel% neq 0 (
    echo ERROR: Failed to start service.
    pause
    exit /b 1
)

echo.
echo Service "%SERVICE_NAME%" installed and started successfully.
pause
endlocal
