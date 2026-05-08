@echo off
setlocal

:: ── Config ──
set PROJECT_DIR=%~dp0
set PUBLISH_DIR=%PROJECT_DIR%bin\Release\net9.0\win-x64\publish

set "YYYY=%date:~10,4%"
set "MM=%date:~4,2%"
set "DD=%date:~7,2%"

set ZIP_NAME=DungeonCrawler%YYYY%-%MM%-%DD%.zip
set ZIP_OUTPUT=%USERPROFILE%\Downloads\%ZIP_NAME%

:: ════════════════════════════════════════
::  OPTIONS — answer Y or N to each prompt
:: ════════════════════════════════════════

echo ========================================
echo   DUNGEON CRAWLER — Publish Options
echo ========================================
echo.

choice /m "Open output folder when done?" /c YN
set OPEN_FOLDER=%ERRORLEVEL%

choice /m "Create a zip in Downloads?" /c YN
set DO_ZIP=%ERRORLEVEL%

echo.

:: ── Clean old build ──
echo [1/4] Cleaning old publish...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if %DO_ZIP%==1 (
    if exist "%ZIP_OUTPUT%" del /q "%ZIP_OUTPUT%"
)

:: ── Publish ──
echo [2/4] Publishing project...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

:: ── Copy Content folder ──
echo [3/4] Copying Content folder...
if exist "%PROJECT_DIR%Content" (
    xcopy /s /e /i /y "%PROJECT_DIR%Content" "%PUBLISH_DIR%\Content" >nul
) else (
    echo WARNING: Content folder not found, skipping...
)

:: ── Copy styles folder ──
echo [4/4] Copying styles folder...
if exist "%PROJECT_DIR%styles" (
    xcopy /s /e /i /y "%PROJECT_DIR%styles" "%PUBLISH_DIR%\styles" >nul
) else (
    echo WARNING: styles folder not found, skipping...
)

:: ── Zip (optional) ──
if %DO_ZIP%==1 (
    echo.
    echo [+] Creating zip...
    powershell -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_OUTPUT%' -Force"
    if %ERRORLEVEL% neq 0 (
        echo ERROR: Zip failed!
        pause
        exit /b 1
    )
    echo     Zip saved to: %ZIP_OUTPUT%
)

:: ── Open output folder with .exe selected (optional) ──
if %OPEN_FOLDER%==1 (
    explorer "%PUBLISH_DIR%"
)

echo.
echo ========================================
echo   BUILD COMPLETE!
echo   Files: %PUBLISH_DIR%
if %DO_ZIP%==1 echo   Zip:   %ZIP_OUTPUT%
echo ========================================
echo.
pause