@echo off
setlocal
cd /d "%~dp0"

if not exist "dist\VoiceType.exe" (
  echo Run publish.bat first.
  pause
  exit /b 1
)

if not exist "dist\runtimes\win-x64\whisper.dll" (
  echo dist is missing runtimes. Run publish.bat again.
  pause
  exit /b 1
)

set "DEST=%USERPROFILE%\Desktop\VoiceType"
if exist "%DEST%" rmdir /s /q "%DEST%"
mkdir "%DEST%"

copy /Y "dist\VoiceType.exe" "%DEST%\" >nul
xcopy /E /I /Y "dist\runtimes" "%DEST%\runtimes" >nul

echo Installed to %DEST%
echo Launch: %DEST%\VoiceType.exe
pause
