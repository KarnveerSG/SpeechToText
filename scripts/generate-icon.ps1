# Generates icon.ico using the GenerateIcon tool
$ErrorActionPreference = "Stop"
$output = Resolve-Path "$PSScriptRoot\..\src\VoiceType\icon.ico"
& "C:\Program Files\dotnet\dotnet.exe" run --project "$PSScriptRoot\..\tools\GenerateIcon\GenerateIcon.csproj" -c Release -- $output
Write-Host "Icon ready at $output"
