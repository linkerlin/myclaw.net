#!/usr/bin/env pwsh
<#
.SYNOPSIS
    MyClaw.NET 启动脚本

.DESCRIPTION
    灵活启动 MyClaw 的各种模式

.PARAMETER Mode
    启动模式:
    - (无参数)  : 同时启动 Gateway (含MCP) + Agent (TUI)
    - all       : 同时启动 Gateway (含MCP) + Agent (TUI)
    - agent     : 交互式对话 (TUI)
    - gateway   : 启动 Gateway 服务 (含 MCP + WebUI)
    - mcp       : 同 gateway (Gateway 包含 MCP)
    - status    : 显示状态
    - onboard   : 首次配置
    - skills    : 技能管理

.PARAMETER Config
    配置文件路径 (默认: config.json)

.PARAMETER Build
    启动前先构建

.PARAMETER Watch
    文件变化时自动重启 (仅 gateway/mcp)

.PARAMETER Detailed
    详细输出

.EXAMPLE
     ./run.ps1              # 启动 GUI/MCP/TUI
     ./run.ps1 agent
     ./run.ps1 gateway
     ./run.ps1 gateway -Config ./myconfig.json
     ./run.ps1 mcp -Build
     ./run.ps1 gateway -Watch
#>

param(
    [Parameter(Position=0)]
    [ValidateSet("agent", "gateway", "mcp", "status", "onboard", "skills", "all")]
    [string]$Mode,

    [string]$Config = "config.json",

    [switch]$Build,
    [switch]$Watch,
    [switch]$Detailed
)

$ErrorActionPreference = "Stop"
$ProjectRoot = $PSScriptRoot
$CliPath = Join-Path $ProjectRoot "src/MyClaw.CLI/bin/Debug/net9.0/MyClaw.CLI.dll"

# 检查是否需要构建
if ($Build -or -not (Test-Path $CliPath)) {
    Write-Host "正在构建..." -ForegroundColor Yellow
    & "$ProjectRoot/build.ps1" -NoTest
    if ($LASTEXITCODE -ne 0) {
        Write-Host "构建失败!" -ForegroundColor Red
        exit 1
    }
}

# 检查配置文件
$ConfigPath = if ([System.IO.Path]::IsPathRooted($Config)) { $Config } else { Join-Path $ProjectRoot $Config }
if (-not (Test-Path $ConfigPath)) {
    Write-Host "配置文件不存在: $ConfigPath" -ForegroundColor Yellow
    Write-Host "使用默认配置..." -ForegroundColor Gray
    $ConfigPath = $null
}

# 构建命令参数 (mcp 模式使用 gateway，因为 MCP 已集成在 gateway 中)
$ActualMode = if ($Mode -eq "mcp") { "gateway" } else { $Mode }
$Args = @($ActualMode)

if ($ConfigPath) {
    $Args += @("--config", $ConfigPath)
}

if ($Verbose) {
    $Args += "--verbose"
}

# 启动
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyClaw.NET - $(if ($Mode) { $Mode } else { 'all' })" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

function Start-MyClaw {
    param([string]$RunMode)
    
    # mcp 模式使用 gateway
    if ($RunMode -eq "mcp") { $RunMode = "gateway" }
    
    $RunArgs = @($RunMode)
    if ($ConfigPath) {
        $RunArgs += @("--config", $ConfigPath)
    }
    if ($Verbose) {
        $RunArgs += "--verbose"
    }
    
    if ($Watch -and ($RunMode -eq "gateway")) {
        $ProjectPath = Join-Path $ProjectRoot "src/MyClaw.CLI/MyClaw.CLI.csproj"
        Write-Host "启动 $RunMode (Watch 模式)..." -ForegroundColor Green
        dotnet watch --project $ProjectPath -- $RunArgs
    } else {
        dotnet $CliPath @RunArgs
    }
}

if (-not $Mode -or $Mode -eq "all") {
    # 同时启动 Gateway (含MCP) + Agent (TUI)
    $jobs = @()
    
    # 启动 Gateway (包含 MCP + WebUI)
    Write-Host "启动 Gateway (含 MCP + WebUI)..." -ForegroundColor Green
    $jobs += Start-Job -ScriptBlock {
        param($Cli, $Config, $Root)
        $args = @("gateway")
        if ($Config) { $args += @("--config", $Config) }
        Set-Location $Root
        dotnet $Cli @args
    } -ArgumentList $CliPath, $ConfigPath, $ProjectRoot
    
    # 等待 Gateway 启动
    Start-Sleep -Seconds 2
    
    # 启动 Agent (TUI) - 前台运行
    Write-Host "启动 Agent (TUI)..." -ForegroundColor Green
    Start-MyClaw "agent"
    
    # 清理后台任务
    $jobs | Stop-Job
    $jobs | Remove-Job
} elseif ($Watch -and ($Mode -eq "gateway" -or $Mode -eq "mcp")) {
    Start-MyClaw "gateway"
} else {
    Start-MyClaw $ActualMode
}
