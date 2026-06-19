@echo off
cd /d "%~dp0"

if not exist "dist\VoiceType.exe" (
  echo VoiceType.exe not found. Run publish.bat first.
  pause
  exit /b 1
)

start "" "dist\VoiceType.exe"
