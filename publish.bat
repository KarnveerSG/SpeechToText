@echo off
setlocal
cd /d "%~dp0"

set "DIST_DIR=dist"

echo Building VoiceType portable package...
if exist "%DIST_DIR%\VoiceType.exe" del /f /q "%DIST_DIR%\VoiceType.exe"
if exist "%DIST_DIR%\runtimes" rmdir /s /q "%DIST_DIR%\runtimes"

dotnet publish src\VoiceType\VoiceType.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o "%DIST_DIR%"

if errorlevel 1 (
  echo Build failed.
  pause
  exit /b 1
)

if not exist "%DIST_DIR%\runtimes\win-x64\whisper.dll" (
  echo Publishing runtimes manually...
  xcopy /E /I /Y "src\VoiceType\bin\Release\net8.0-windows10.0.19041.0\win-x64\runtimes" "%DIST_DIR%\runtimes" >nul
)

if not exist "%DIST_DIR%\runtimes\win-x64\whisper.dll" (
  echo ERROR: Whisper runtimes were not published beside VoiceType.exe.
  pause
  exit /b 1
)

echo.
echo Done. Portable package: %DIST_DIR%
echo   VoiceType.exe
echo   runtimes\win-x64\  ^(4 DLLs - REQUIRED^)
echo.
echo Copy the whole dist folder to Desktop, or at minimum exe + runtimes folder.
pause
