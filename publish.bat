@echo off
setlocal

:: ── Config ──
set PROJECT_DIR=%~dp0
set PUBLISH_DIR=%PROJECT_DIR%\bin\Release\net9.0\win-x64\publish

set "YYYY=%date:~10,4%"
set "MM=%date:~4,2%"
set "DD=%date:~7,2%"

set ZIP_NAME=DungeonCrawler%YYYY%-%MM%-%DD%.zip
set ZIP_OUTPUT=%USERPROFILE%\Downloads\%ZIP_NAME%

:: ── Clean old build ──
echo [1/5] Cleaning old publish...
if exist "%PUBLISH_DIR%" rmdir /s /q "%PUBLISH_DIR%"
if exist "%ZIP_OUTPUT%" del /q "%ZIP_OUTPUT%"

:: ── Publish ──
echo [2/5] Publishing project...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
if %ERRORLEVEL% neq 0 (
    echo ERROR: Publish failed!
    pause
    exit /b 1
)

:: ── Copy Content folder ──
echo [3/5] Copying Content folder...
if exist "%PROJECT_DIR%Content" (
    xcopy /s /e /i /y "%PROJECT_DIR%Content" "%PUBLISH_DIR%Content"
) else (
    echo WARNING: Content folder not found, skipping...
)

:: ── Copy styles folder ──
echo [4/5] Copying styles folder...
if exist "%PROJECT_DIR%styles" (
    xcopy /s /e /i /y "%PROJECT_DIR%styles" "%PUBLISH_DIR%\styles"
) else (
    echo WARNING: styles folder not found, skipping...
)

:: ── Zip it up ──
echo [5/5] Creating zip...
powershell -Command "Compress-Archive -Path '%PUBLISH_DIR%\*' -DestinationPath '%ZIP_OUTPUT%' -Force"
if %ERRORLEVEL% neq 0 (
    echo ERROR: Zip failed!
    pause
    exit /b 1
)

echo.
echo ========================================
echo   BUILD COMPLETE!
echo   Zip: %ZIP_OUTPUT%
echo ========================================
echo.
pause