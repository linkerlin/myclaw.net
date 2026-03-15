# MyClaw.NET 一键安装脚本 (Windows)
# 用法: Invoke-Expression (Invoke-WebRequest -Uri "https://raw.githubusercontent.com/your-org/myclaw.net/main/scripts/install.ps1").Content

#Requires -Version 5.1

[CmdletBinding()]
param(
    [string]$Version = "latest",
    [string]$InstallDir = "$env:USERPROFILE\.local\bin",
    [switch]$Help
)

# 配置
$Script:Repo = "your-org/myclaw.net"
$Script:ErrorActionPreference = "Stop"

# 颜色输出函数
function Write-Info {
    param([string]$Message)
    Write-Host "[INFO] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[SUCCESS] $Message" -ForegroundColor Green
}

function Write-Warning {
    param([string]$Message)
    Write-Host "[WARNING] $Message" -ForegroundColor Yellow
}

function Write-Error {
    param([string]$Message)
    Write-Host "[ERROR] $Message" -ForegroundColor Red
}

# 显示帮助
if ($Help) {
    Write-Host @"
MyClaw.NET 安装脚本 (Windows)

用法:
    install.ps1 [选项]

选项:
    -Version <版本>     指定版本 (默认: latest)
    -InstallDir <目录>  安装目录 (默认: ~\.local\bin)
    -Help               显示此帮助

示例:
    # 安装最新版本
    .\install.ps1

    # 安装指定版本
    .\install.ps1 -Version "v1.0.0"

    # 安装到指定目录
    .\install.ps1 -InstallDir "C:\Tools"

一键安装 (PowerShell):
    Invoke-Expression (Invoke-WebRequest -Uri "https://raw.githubusercontent.com/your-org/myclaw.net/main/scripts/install.ps1").Content
"@
    exit 0
}

# 检测平台
function Get-Platform {
    $os = "windows"
    
    # 检测架构
    $arch = switch ($env:PROCESSOR_ARCHITECTURE) {
        "AMD64" { "x64" }
        "x86"   { "x86" }
        "ARM64" { "arm64" }
        default { 
            Write-Error "不支持的架构: $env:PROCESSOR_ARCHITECTURE"
            exit 1
        }
    }
    
    return "$os-$arch"
}

# 检查依赖
function Test-Dependencies {
    $required = @("Invoke-WebRequest", "New-Item", "Move-Item")
    
    foreach ($cmd in $required) {
        if (-not (Get-Command $cmd -ErrorAction SilentlyContinue)) {
            Write-Error "缺少必要的 PowerShell 功能: $cmd"
            exit 1
        }
    }
}

# 下载二进制文件
function Download-Binary {
    param(
        [string]$Platform,
        [string]$Version,
        [string]$InstallDir
    )
    
    $binaryName = "myclaw.exe"
    
    # 构建 URL
    $versionTag = if ($Version -eq "latest") {
        "latest/download"
    } else {
        "download/$Version"
    }
    
    $url = "https://github.com/$Repo/releases/$versionTag/myclaw-$Platform.exe"
    $outputPath = Join-Path $InstallDir $binaryName
    $tempPath = "$outputPath.tmp"
    
    Write-Info "下载 MyClaw.NET $Version for $Platform..."
    Write-Info "URL: $url"
    
    # 创建安装目录
    if (-not (Test-Path $InstallDir)) {
        Write-Info "创建安装目录: $InstallDir"
        New-Item -ItemType Directory -Force -Path $InstallDir | Out-Null
    }
    
    # 下载
    try {
        $ProgressPreference = "Continue"
        Invoke-WebRequest -Uri $url -OutFile $tempPath -UseBasicParsing
        
        # 移动文件
        if (Test-Path $outputPath) {
            Remove-Item $outputPath -Force
        }
        Move-Item $tempPath $outputPath -Force
    }
    catch {
        if (Test-Path $tempPath) {
            Remove-Item $tempPath -Force -ErrorAction SilentlyContinue
        }
        Write-Error "下载失败: $_"
        exit 1
    }
    
    Write-Success "下载完成: $outputPath"
    return $outputPath
}

# 验证安装
function Test-Installation {
    param([string]$BinaryPath)
    
    if (-not (Test-Path $BinaryPath)) {
        Write-Error "安装验证失败: 找不到二进制文件"
        exit 1
    }
    
    # 尝试获取版本
    try {
        $versionOutput = & $BinaryPath --version 2>$null
        if ($versionOutput) {
            Write-Success "MyClaw.NET $versionOutput 安装成功!"
        } else {
            Write-Warning "无法验证版本，但二进制文件已安装"
        }
    }
    catch {
        Write-Warning "无法验证版本，但二进制文件已安装"
    }
}

# 检查 PATH
function Test-PathEnv {
    param([string]$InstallDir)
    
    $pathDirs = $env:PATH -split ";"
    $inPath = $false
    
    foreach ($dir in $pathDirs) {
        if ($dir -eq $InstallDir -or $dir -eq (Resolve-Path $InstallDir -ErrorAction SilentlyContinue).Path) {
            $inPath = $true
            break
        }
    }
    
    if (-not $inPath) {
        Write-Warning "$InstallDir 不在 PATH 中"
        Write-Host ""
        Write-Host "请将以下路径添加到系统 PATH:"
        Write-Host "    $InstallDir"
        Write-Host ""
        Write-Host "方法一 (当前用户):"
        Write-Host "    [Environment]::SetEnvironmentVariable(\"PATH\", \"`$env:PATH;$InstallDir\", \"User\")"
        Write-Host ""
        Write-Host "方法二 (手动):"
        Write-Host "    1. 右键'此电脑' -> 属性 -> 高级系统设置"
        Write-Host "    2. 环境变量 -> 编辑 PATH"
        Write-Host "    3. 添加新条目: $InstallDir"
    }
}

# 打印使用说明
function Show-Usage {
    Write-Host ""
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "MyClaw.NET 安装完成!" -ForegroundColor Green
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "使用说明:"
    Write-Host "  myclaw --help          显示帮助信息"
    Write-Host "  myclaw status          查看系统状态"
    Write-Host "  myclaw onboard         初始化配置"
    Write-Host ""
    Write-Host "MCP 服务:"
    Write-Host "  myclaw mcp             启动 MCP 服务"
    Write-Host ""
    Write-Host "更多信息:"
    Write-Host "  https://github.com/$Repo"
    Write-Host "==========================================" -ForegroundColor Cyan
}

# 主函数
function Main {
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host "MyClaw.NET 安装脚本 (Windows)" -ForegroundColor Cyan
    Write-Host "==========================================" -ForegroundColor Cyan
    Write-Host ""
    
    # 检测平台
    Write-Info "检测平台..."
    $platform = Get-Platform
    Write-Success "检测到平台: $platform"
    
    # 检查依赖
    Write-Info "检查依赖..."
    Test-Dependencies
    Write-Success "依赖检查通过"
    
    # 下载
    $binaryPath = Download-Binary -Platform $platform -Version $Version -InstallDir $InstallDir
    
    # 验证
    Test-Installation -BinaryPath $binaryPath
    
    # 检查 PATH
    Test-PathEnv -InstallDir $InstallDir
    
    # 打印使用说明
    Show-Usage
}

# 运行主函数
Main
