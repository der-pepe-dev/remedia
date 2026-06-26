param(
    [string]$ToolsRoot = "$PSScriptRoot\..\tools"
)

$ErrorActionPreference = "Stop"

Write-Host "ReMedia Sync tool setup helper"
Write-Host "Place ffmpeg/ffprobe under: $ToolsRoot"

New-Item -ItemType Directory -Force -Path $ToolsRoot | Out-Null

Write-Host "This script is intentionally conservative."
Write-Host "Download the preferred ffmpeg build you trust, extract it under the tools folder,"
Write-Host "and point the app/CLI configuration at the resulting ffmpeg.exe and ffprobe.exe paths."
