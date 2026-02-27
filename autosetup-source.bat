@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_NAME=%~nx0"
set "SRC_DIR=%~dp0"
set "TARGET_DIR="

echo Detecting Among Us installation...

if exist "%ProgramFiles(x86)%\Steam\steamapps\common\Among Us" (
    set "TARGET_DIR=%ProgramFiles(x86)%\Steam\steamapps\common\Among Us"
)

if not defined TARGET_DIR if exist "%ProgramFiles%\Steam\steamapps\common\Among Us" (
    set "TARGET_DIR=%ProgramFiles%\Steam\steamapps\common\Among Us"
)

if not defined TARGET_DIR call :TRY_REG "HKLM\SOFTWARE\WOW6432Node\Valve\Steam"
if not defined TARGET_DIR call :TRY_REG "HKLM\SOFTWARE\Valve\Steam"
if not defined TARGET_DIR call :TRY_REG "HKCU\SOFTWARE\Valve\Steam"

if not defined TARGET_DIR (
    echo Could not detect Among Us folder automatically.
    set /p TARGET_DIR=Paste your Among Us folder path and press Enter: 
)

if not exist "%TARGET_DIR%" (
    echo.
    echo Invalid destination: "%TARGET_DIR%"
    pause
    exit /b 1
)

echo.
echo Copying files to:
echo "%TARGET_DIR%"
echo.

robocopy "%SRC_DIR%" "%TARGET_DIR%" /E /R:1 /W:1 /XF "%SCRIPT_NAME%" >nul
set "RC=%ERRORLEVEL%"

if %RC% GEQ 8 (
    echo Copy failed with robocopy exit code %RC%.
    pause
    exit /b %RC%
)

echo Open among us and play!
pause
exit /b 0

:TRY_REG
set "STEAM_ROOT="
for /f "tokens=2,*" %%A in ('reg query "%~1" /v InstallPath 2^>nul ^| find /I "InstallPath"') do set "STEAM_ROOT=%%B"
if not defined STEAM_ROOT for /f "tokens=2,*" %%A in ('reg query "%~1" /v SteamPath 2^>nul ^| find /I "SteamPath"') do set "STEAM_ROOT=%%B"
if defined STEAM_ROOT (
    if exist "!STEAM_ROOT!\steamapps\common\Among Us" (
        set "TARGET_DIR=!STEAM_ROOT!\steamapps\common\Among Us"
    )
)
exit /b 0

