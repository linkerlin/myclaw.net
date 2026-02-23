# fix-lock.ps1 - 解决 Windows 构建时的文件锁定问题
# 使用方法: 在项目根目录运行 .\fix-lock.ps1

param(
    [switch]$Rebuild = $true,
    [switch]$KillProcesses = $true,
    [switch]$CleanBinObj = $true
)

$ErrorActionPreference = "SilentlyContinue"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  MyClaw 文件锁定修复工具" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# 1. 杀死残留进程
if ($KillProcesses) {
    Write-Host "🔪 正在杀死残留进程..." -ForegroundColor Yellow
    
    # 杀死 myclaw 相关进程
    $processes = Get-Process | Where-Object { 
        $_.Name -like "*myclaw*" -or 
        $_.Name -like "*dotnet*" -or
        $_.Name -like "*Agent*"
    }
    
    foreach ($proc in $processes) {
        try {
            Write-Host "  终止进程: $($proc.Name) (PID: $($proc.Id))" -ForegroundColor DarkGray
            $proc | Stop-Process -Force
        } catch {
            Write-Host "  无法终止: $($proc.Name) - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
    
    # 使用 taskkill 作为备选
    taskkill /F /IM dotnet.exe 2>$null | Out-Null
    taskkill /F /IM myclaw.exe 2>$null | Out-Null
    
    Write-Host "✅ 进程清理完成" -ForegroundColor Green
    Write-Host ""
}

# 2. 检查端口占用
Write-Host "🔍 检查端口占用..." -ForegroundColor Yellow
$ports = @(5000, 5001, 8080)
foreach ($port in $ports) {
    $connection = netstat -ano | findstr ":$port"
    if ($connection) {
        Write-Host "  端口 $port 被占用:" -ForegroundColor Red
        $connection | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkRed }
        
        # 尝试提取 PID 并杀死
        $line = $connection | Select-Object -First 1
        if ($line -match "(\d+)\s*$") {
            $pid = $matches[1]
            Write-Host "  尝试终止 PID: $pid" -ForegroundColor DarkYellow
            taskkill /PID $pid /F 2>$null | Out-Null
        }
    }
}
Write-Host ""

# 3. 清理 bin/obj 目录
if ($CleanBinObj) {
    Write-Host "🧹 清理构建缓存..." -ForegroundColor Yellow
    
    $binDirs = Get-ChildItem -Path . -Recurse -Directory -Filter "bin" -ErrorAction SilentlyContinue
    $objDirs = Get-ChildItem -Path . -Recurse -Directory -Filter "obj" -ErrorAction SilentlyContinue
    
    $totalDirs = ($binDirs.Count + $objDirs.Count)
    Write-Host "  发现 $totalDirs 个缓存目录" -ForegroundColor DarkGray
    
    foreach ($dir in ($binDirs + $objDirs)) {
        try {
            Remove-Item -Path $dir.FullName -Recurse -Force
            Write-Host "  已删除: $($dir.FullName)" -ForegroundColor DarkGray
        } catch {
            Write-Host "  ⚠️ 无法删除: $($dir.FullName)" -ForegroundColor Red
        }
    }
    
    Write-Host "✅ 缓存清理完成" -ForegroundColor Green
    Write-Host ""
}

# 4. 重新构建
if ($Rebuild) {
    Write-Host "🔨 开始重新构建..." -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host "  → dotnet restore" -ForegroundColor Cyan
    dotnet restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 还原失败！" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "  → dotnet build" -ForegroundColor Cyan
    dotnet build
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ 构建失败！" -ForegroundColor Red
        exit 1
    }
    
    Write-Host ""
    Write-Host "✅ 构建成功！" -ForegroundColor Green
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  修复完成，可以重新运行程序了" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
