@echo off
setlocal EnableExtensions EnableDelayedExpansion

set "SCRIPT_DIR=%~dp0"
set "TEMP_DIR=%SCRIPT_DIR%Temp"
set "LIMIT=500"
set "KEEP_COUNT=300"

if not exist "%TEMP_DIR%\" (
    exit /b 0
)

for /f %%C in ('dir /b /a-d "%TEMP_DIR%\*.md" 2^>nul ^| find /c /v ""') do set "DOC_COUNT=%%C"

if not defined DOC_COUNT set "DOC_COUNT=0"
if %DOC_COUNT% LEQ %LIMIT% (
    exit /b 0
)

set /a DELETE_COUNT=DOC_COUNT-KEEP_COUNT

for /f "delims=" %%F in ('dir /b /a-d /o:d /t:c "%TEMP_DIR%\*.md" 2^>nul') do (
    if !DELETE_COUNT! LEQ 0 exit /b 0
    del /q "%TEMP_DIR%\%%F" >nul 2>nul
    set /a DELETE_COUNT-=1
    if !DELETE_COUNT! LEQ 0 exit /b 0
)

exit /b 0
