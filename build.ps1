#!/usr/bin/env pwsh
<#
.SYNOPSIS
    MyClaw.NET 构建脚本

.DESCRIPTION
    一键构建所有项目，包括清理、还原、编译、测试

.PARAMETER Configuration
    构建配置 (Debug/Release)，默认 Debug

.PARAMETER NoTest
    跳过测试

.PARAMETER Clean
    构建前清理输出目录

.EXAMPLE
    ./build.ps1
    ./build.ps1 -Configuration Release
    ./build.ps1 -Clean -NoTest
#>

param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Debug",
    [switch]$NoTest,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"
$StartTime = Get-Date

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyClaw.NET Build Script" -ForegroundColor Cyan
Write-Host "  Configuration: $Configuration" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 清理
if ($Clean) {
    Write-Host "[1/4] 清理输出目录..." -ForegroundColor Yellow
    Get-ChildItem -Path . -Directory -Recurse -Filter "bin" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Get-ChildItem -Path . -Directory -Recurse -Filter "obj" | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host "  清理完成" -ForegroundColor Green
    Write-Host ""
}

# 还原
Write-Host "[2/4] 还原 NuGet 包..." -ForegroundColor Yellow
dotnet restore --locked-mode
if ($LASTEXITCODE -ne 0) {
    Write-Host "  还原失败!" -ForegroundColor Red
    exit 1
}
Write-Host "  还原完成" -ForegroundColor Green
Write-Host ""

# 编译
Write-Host "[3/4] 编译项目..." -ForegroundColor Yellow
dotnet build -c $Configuration --no-restore -v minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "  编译失败!" -ForegroundColor Red
    exit 1
}
Write-Host "  编译完成" -ForegroundColor Green
Write-Host ""

# 测试
if (-not $NoTest) {
    Write-Host "[4/4] 运行测试..." -ForegroundColor Yellow
    dotnet test -c $Configuration --no-build --verbosity minimal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  测试失败!" -ForegroundColor Red
        exit 1
    }
    Write-Host "  测试完成" -ForegroundColor Green
} else {
    Write-Host "[4/4] 跳过测试" -ForegroundColor Gray
}
Write-Host ""

$EndTime = Get-Date
$Duration = $EndTime - $StartTime

Write-Host "========================================" -ForegroundColor Green
Write-Host "  构建成功! 耗时: $($Duration.ToString('mm\:ss'))" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "输出目录:" -ForegroundColor White
Write-Host "  CLI:  src/MyClaw.CLI/bin/$Configuration/net9.0/" -ForegroundColor Gray
Write-Host "  MCP:  src/MyClaw.MCP/bin/$Configuration/net9.0/" -ForegroundColor Gray
