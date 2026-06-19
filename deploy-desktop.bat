@echo off
setlocal
cd /d "%~dp0"

set "DESKTOP=%USERPROFILE%\Desktop"
set "EXE_NAME=VoiceType.exe"

echo Building VoiceType.exe...
dotnet publish src\VoiceType\VoiceType.csproj ^
  -c Release ^
  -r win-x64 ^
  --self-contained true ^
  -p:PublishSingleFile=true ^
  -p:IncludeNativeLibrariesForSelfExtract=true ^
  -p:EnableCompressionInSingleFile=true ^
  -o dist

if errorlevel 1 (
  echo Build failed.
  exit /b 1
)

if not exist "dist\%EXE_NAME%" (
  echo dist\%EXE_NAME% was not created.
  exit /b 1
)

if exist "%DESKTOP%\%EXE_NAME%" del /f /q "%DESKTOP%\%EXE_NAME%"
if exist "%DESKTOP%\VoiceType" rmdir /s /q "%DESKTOP%\VoiceType"
if exist "%DESKTOP%\runtimes" rmdir /s /q "%DESKTOP%\runtimes"

taskkill /IM VoiceType.exe /F >nul 2>&1

copy /Y "dist\%EXE_NAME%" "%DESKTOP%\%EXE_NAME%" >nul

echo.
echo Installed: %DESKTOP%\%EXE_NAME%
echo Whisper natives extract to %%APPDATA%%\VoiceType\runtimes on first run.
