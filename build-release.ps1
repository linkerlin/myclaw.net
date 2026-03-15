#!/usr/bin/env pwsh
<#
.SYNOPSIS
    MyClaw.NET Single-File Release Build Script

.DESCRIPTION
    Build self-contained single-file executables (CLI and MCP) to ./publish/ directory

.PARAMETER Configuration
    Build configuration (Debug/Release), default: Release

.PARAMETER Runtime
    Target runtime (win-x64/win-arm64/linux-x64/linux-arm64/osx-x64/osx-arm64)
    Default: win-x64 (Windows x64)

.PARAMETER Version
    Version number to embed in executables

.EXAMPLE
    ./build-release.ps1
    ./build-release.ps1 -Runtime linux-x64
    ./build-release.ps1 -Configuration Release -Version 1.0.0
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    
    [string]$Runtime = "win-x64",
    
    [string]$Version = ""
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

$exeExt = if ($Runtime -like "win-*") { ".exe" } else { "" }

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyClaw.NET Release Build" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "  Runtime: $Runtime" -ForegroundColor Cyan
if ($Version) {
    Write-Host "  Version: $Version" -ForegroundColor Cyan
}
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Clean
Write-Host "[1/3] Cleaning output directory..." -ForegroundColor Yellow
$publishDir = "./publish"
if (Test-Path $publishDir) {
    Remove-Item -Path $publishDir -Recurse -Force
}
New-Item -ItemType Directory -Path $publishDir | Out-Null
Write-Host "  Clean complete" -ForegroundColor Green
Write-Host ""

# Build parameters
$buildArgs = @(
    "-c", $Configuration
    "-r", $Runtime
    "--self-contained", "true"
    "-p:PublishSingleFile=true"
    "-p:PublishTrimmed=false"
    "-p:IncludeNativeLibrariesForSelfExtract=true"
    "-p:EnableCompressionInSingleFile=true"
)

if ($Version) {
    $buildArgs += "-p:Version=$Version"
    $buildArgs += "-p:AssemblyVersion=$Version"
}

# Build CLI
Write-Host "[2/3] Building CLI (single-file)..." -ForegroundColor Yellow
$cliOutput = "$publishDir/cli"
New-Item -ItemType Directory -Path $cliOutput -Force | Out-Null

dotnet publish src/MyClaw.CLI/MyClaw.CLI.csproj @buildArgs -o $cliOutput -v minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "  CLI build failed!" -ForegroundColor Red
    exit 1
}

# Rename CLI
$cliFinalName = "myclaw-$Runtime$exeExt"
Move-Item -Path "$cliOutput/MyClaw.CLI$exeExt" -Destination "$publishDir/$cliFinalName" -Force
Copy-Item -Path "$cliOutput/*.json" -Destination $publishDir -ErrorAction SilentlyContinue
Remove-Item -Path $cliOutput -Recurse -Force

Write-Host "  -> $cliFinalName" -ForegroundColor Green
Write-Host ""

# Build MCP
Write-Host "[3/3] Building MCP (single-file)..." -ForegroundColor Yellow
$mcpOutput = "$publishDir/mcp"
New-Item -ItemType Directory -Path $mcpOutput -Force | Out-Null

dotnet publish src/MyClaw.MCP/MyClaw.MCP.csproj @buildArgs -o $mcpOutput -v minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "  MCP build failed!" -ForegroundColor Red
    exit 1
}

# Rename MCP
$mcpFinalName = "myclaw-mcp-$Runtime$exeExt"
Move-Item -Path "$mcpOutput/MyClaw.MCP$exeExt" -Destination "$publishDir/$mcpFinalName" -Force
Get-ChildItem -Path "$mcpOutput/*.json" | Where-Object { 
    -not (Test-Path "$publishDir/$($_.Name)") 
} | Copy-Item -Destination $publishDir -ErrorAction SilentlyContinue
Remove-Item -Path $mcpOutput -Recurse -Force

Write-Host "  -> $mcpFinalName" -ForegroundColor Green
Write-Host ""

# Version info
$versionInfo = @{
    BuildTime = (Get-Date -Format "yyyy-MM-dd HH:mm:ss")
    Runtime = $Runtime
    Configuration = $Configuration
    Version = if ($Version) { $Version } else { "dev" }
} | ConvertTo-Json

$versionInfo | Out-File -FilePath "$publishDir/version.json" -Encoding UTF8

$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host "========================================" -ForegroundColor Green
Write-Host "  Release build successful!" -ForegroundColor Green
Write-Host "  Duration: $($Duration.ToString('mm\:ss'))" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "Output directory: $((Resolve-Path $publishDir).Path)" -ForegroundColor White
Write-Host ""

# List files
$files = Get-ChildItem -Path $publishDir -File | Sort-Object Length -Descending
Write-Host "Generated files:" -ForegroundColor White
foreach ($file in $files) {
    $size = if ($file.Length -gt 1MB) { 
        "{0:N1} MB" -f ($file.Length / 1MB) 
    } else { 
        "{0:N1} KB" -f ($file.Length / 1KB) 
    }
    Write-Host "  $($file.Name.PadRight(35)) $size" -ForegroundColor Gray
}

Write-Host ""
Write-Host "Usage:" -ForegroundColor White
Write-Host "  ./publish/$cliFinalName --help" -ForegroundColor Gray
Write-Host "  ./publish/$mcpFinalName" -ForegroundColor Gray
Write-Host ""
