@echo off
cd /d "%~dp0"
dotnet run --project src\VoiceType\VoiceType.csproj -c Release
if errorlevel 1 pause
