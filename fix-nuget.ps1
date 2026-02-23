# fix-nuget.ps1 - 强力修复 NuGet 包锁定问题

Write-Host "========================================" -ForegroundColor Red
Write-Host "  NuGet 包锁定强力修复工具" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""

# 1. 杀死所有相关进程
Write-Host "正在杀死所有相关进程..." -ForegroundColor Yellow
$processes = @("dotnet", "VBCSCompiler", "MSBuild", "devenv")
foreach ($proc in $processes) {
    Get-Process -Name $proc -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
            Write-Host "  已终止: $proc (PID: $($_.Id))" -ForegroundColor DarkGray
        } catch {}
    }
}
Start-Sleep -Seconds 3

# 2. 再次检查并强制终止
Write-Host "再次检查残留进程..." -ForegroundColor Yellow
Get-Process | Where-Object { 
    $_.ProcessName -match "dotnet|VBCS|MSBuild" 
} | ForEach-Object {
    try {
        Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue
        Write-Host "  强制终止 PID: $($_.Id)" -ForegroundColor DarkGray
    } catch {}
}
Start-Sleep -Seconds 2

# 3. 清理 NuGet 包缓存
Write-Host "清理 NuGet 包缓存..." -ForegroundColor Yellow
$packagesToClean = @(
    "system.text.json",
    "system.text.json.sourcegeneration",
    "microsoft.extensions.options",
    "microsoft.extensions.logging.generators",
    "microsoft.extensions.options.sourcegeneration",
    "agentscope.core"
)

$nugetPath = "$env:USERPROFILE\.nuget\packages"
foreach ($pkg in $packagesToClean) {
    $pkgPath = Join-Path $nugetPath $pkg
    if (Test-Path $pkgPath) {
        try {
            $tempPath = "$pkgPath`_old_$(Get-Random)"
            Rename-Item -Path $pkgPath -NewName $tempPath -Force -ErrorAction SilentlyContinue
            Remove-Item -Path $tempPath -Recurse -Force -ErrorAction SilentlyContinue
            Write-Host "  已清理: $pkg" -ForegroundColor Green
        } catch {
            Write-Host "  无法清理: $pkg - $($_.Exception.Message)" -ForegroundColor Red
        }
    }
}

# 4. 清理项目 bin/obj
Write-Host "清理项目 bin/obj..." -ForegroundColor Yellow
cd C:\GitHub\myclaw.net
Get-ChildItem -Path . -Recurse -Directory -Filter "bin" | ForEach-Object {
    try {
        Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    } catch {}
}
Get-ChildItem -Path . -Recurse -Directory -Filter "obj" | ForEach-Object {
    try {
        Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
    } catch {}
}

# 5. 清理 NuGet 临时文件
Write-Host "清理 NuGet 临时文件..." -ForegroundColor Yellow
$tempPaths = @(
    "$env:TEMP\NuGet*",
    "$env:TEMP\dotnet*",
    "$env:LOCALAPPDATA\NuGet\v3-cache",
    "$env:LOCALAPPDATA\Temp\NuGet*"
)
foreach ($path in $tempPaths) {
    Get-Item -Path $path -ErrorAction SilentlyContinue | ForEach-Object {
        try {
            Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction SilentlyContinue
        } catch {}
    }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host "  清理完成！请重新运行:" -ForegroundColor Green
Write-Host "    dotnet restore" -ForegroundColor Cyan
Write-Host "    dotnet build" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Green
