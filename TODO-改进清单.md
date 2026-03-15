# MyClaw.NET 改进清单

> 基于 MiniClaw 深度对比后的具体行动项

---

## ✅ 已完成 (无需改动)

| 模块 | 文件 | 说明 |
|------|------|------|
| 工作区检测 | `WorkspaceDetector.cs` | Git状态、技术栈检测 ✅ |
| 技术栈识别 | `TechStackDetector.cs` | 50+技术 ✅ |
| 使用统计 | `AnalyticsService.cs` | 工具/启动/技能统计 ✅ |
| 每日简报 | `DailyBriefingService.cs` | 昨日回顾/统计/实体 ✅ |
| 上下文编译 | `ContextCompiler.cs` | Token预算/优先级 ✅ |
| 情感系统 | `AffectManager.cs` | 4维模型 ✅ |
| 进化引擎 | `EvolutionEngine.cs` | 模式检测/DNA更新 ✅ |
| 痛觉记忆 | `NociceptionManager.cs` | 指数衰减 ✅ |
| 好奇心引擎 | `CuriosityEngine.cs` | 6种目标 ✅ |
| 向量记忆 | `VectorMemory/` | RAG检索 ✅ |

---

## 🔨 待办事项

### 🔴 高优先级 (本周)

#### 1. 添加项目类型检测
```csharp
// 新文件: src/MyClaw.Core/Workspace/ProjectTypeDetector.cs
public enum ProjectType { React, Vue, Node, Go, Python, DotNet, Rust, Java, Unknown }

public static class ProjectTypeDetector
{
    public static ProjectType Detect(string workspacePath)
    {
        var techStack = TechStackDetector.Detect(workspacePath);
        
        if (techStack.Contains("React")) return ProjectType.React;
        if (techStack.Contains("Vue")) return ProjectType.Vue;
        if (techStack.Contains("Go")) return ProjectType.Go;
        if (techStack.Contains("Python")) return ProjectType.Python;
        if (techStack.Contains(".NET") || techStack.Contains("C#")) return ProjectType.DotNet;
        if (techStack.Contains("Rust")) return ProjectType.Rust;
        if (techStack.Contains("Java")) return ProjectType.Java;
        if (techStack.Contains("Node.js")) return ProjectType.Node;
        
        return ProjectType.Unknown;
    }
}
```

#### 2. 优化上下文输出格式
```csharp
// 修改: src/MyClaw.Core/Workspace/WorkspaceInfo.cs
public string ToContextString()
{
    var parts = new List<string>();
    
    // 项目类型和名称
    if (ProjectType != ProjectType.Unknown)
        parts.Add($"Project: {Name} ({ProjectType})");
    else
        parts.Add($"Project: {Name}");
    
    // 路径
    parts.Add($"Path: {Path}");
    
    // Git
    if (Git.IsRepo)
    {
        var gitPart = $"Git: {Git.Branch}";
        if (Git.UncommittedChanges > 0)
            gitPart += $" | dirty (+{Git.UncommittedChanges} files)";
        parts.Add(gitPart);
    }
    
    // 技术栈
    if (TechStack.Count > 0)
        parts.Add($"Stack: {string.Join(", ", TechStack.Take(5))}");
    
    return string.Join(" | ", parts);
}
```

#### 3. 一键安装脚本
```bash
# 新文件: scripts/install.sh
#!/bin/bash
set -e

REPO="your-org/myclaw.net"
INSTALL_DIR="${HOME}/.local/bin"

# 检测平台
OS=$(uname -s | tr '[:upper:]' '[:lower:]')
ARCH=$(uname -m)
case "$ARCH" in
    x86_64) ARCH="x64" ;;
    arm64|aarch64) ARCH="arm64" ;;
esac

echo "Installing myclaw for $OS-$ARCH..."

# 下载最新版本
URL="https://github.com/$REPO/releases/latest/download/myclaw-$OS-$ARCH"
mkdir -p "$INSTALL_DIR"
curl -L "$URL" -o "$INSTALL_DIR/myclaw"
chmod +x "$INSTALL_DIR/myclaw"

echo "✅ Installed to $INSTALL_DIR/myclaw"
echo "Make sure $INSTALL_DIR is in your PATH"
```

### 🟡 中优先级 (下周)

#### 4. GitHub Actions 自动发布
```yaml
# 新文件: .github/workflows/release.yml
name: Release
on:
  push:
    tags: ['v*']
jobs:
  publish:
    runs-on: ubuntu-latest
    strategy:
      matrix:
        rid: [linux-x64, win-x64, osx-x64, osx-arm64]
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '9.0.x'
      - run: dotnet publish src/MyClaw.CLI -c Release -r ${{ matrix.rid }} --self-contained -p:PublishSingleFile=true -o ./publish
      - run: mv ./publish/MyClaw.CLI${{ matrix.rid == 'win-x64' && '.exe' || '' }} ./publish/myclaw-${{ matrix.rid }}${{ matrix.rid == 'win-x64' && '.exe' || '' }}
      - uses: softprops/action-gh-release@v1
        with:
          files: ./publish/myclaw-*
```

#### 5. 技能缓存 (5s TTL)
```csharp
// 修改: src/MyClaw.Skills/SkillManager.cs
public class SkillManager
{
    private readonly Dictionary<string, (Skill skill, DateTime cachedAt)> _cache = new();
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);
    
    public Skill? GetSkill(string name)
    {
        // 检查缓存
        if (_cache.TryGetValue(name, out var cached))
        {
            if (DateTime.Now - cached.cachedAt < CacheTtl)
                return cached.skill;
            _cache.Remove(name);
        }
        
        // 加载并缓存
        var skill = LoadFromDisk(name);
        if (skill != null)
            _cache[name] = (skill, DateTime.Now);
        
        return skill;
    }
}
```

#### 6. AI CLI 检测器
```csharp
// 新文件: src/MyClaw.Heartbeat/AiCliDetector.cs
public class AiCliDetector
{
    private readonly List<AiCliInfo> _knownClis = new()
    {
        new("claude", "claude --version"),
        new("gemini", "gemini --version"),
        new("kimi", "kimi --version"),
        new("aider", "aider --version")
    };
    
    public async Task<List<AiCliInfo>> DetectAvailableClisAsync()
    {
        var available = new List<AiCliInfo>();
        foreach (var cli in _knownClis)
        {
            if (await IsAvailableAsync(cli))
                available.Add(cli);
        }
        return available;
    }
}
```

### 🟢 低优先级 (可选)

- [ ] 进化算法语义分析增强
- [ ] 文件健康检查 (DNA更新频率)
- [ ] 心跳自主执行完整实现

---

## 📊 进度追踪

| 任务 | 状态 | 优先级 | 预计工时 | 负责人 |
|------|------|--------|----------|--------|
| 项目类型检测 | ⏳ 待开始 | 🔴 高 | 4h | TBD |
| 上下文格式优化 | ⏳ 待开始 | 🔴 高 | 2h | TBD |
| 一键安装脚本 | ⏳ 待开始 | 🔴 高 | 4h | TBD |
| GitHub Actions发布 | ⏳ 待开始 | 🟡 中 | 4h | TBD |
| 技能缓存 | ⏳ 待开始 | 🟡 中 | 2h | TBD |
| AI CLI检测 | ⏳ 待开始 | 🟡 中 | 4h | TBD |

---

## 🎯 验收标准

### 项目类型检测
- [ ] 正确识别 React/Vue/Node/Go/Python/DotNet/Rust/Java 项目
- [ ] 集成到 `WorkspaceInfo.ToContextString()`
- [ ] 输出格式: `Project: my-app (React)`

### 上下文格式优化
- [ ] 输出包含: 项目名称、路径、Git分支、技术栈
- [ ] 格式: `Project: X | Path: Y | Git: Z | Stack: A, B`
- [ ] 脏状态显示: `dirty (+N files)`

### 一键安装
- [ ] 脚本自动检测平台
- [ ] 下载对应架构的二进制
- [ ] 安装到 `~/.local/bin`
- [ ] 提供使用说明

---

## 📚 参考文档

- [详细改进方案](./改进方案和计划-v3-深度对比.md)
- [执行摘要 v2](./改进方案-执行摘要-v2.md)
- [MiniClaw 对比分析](./docs/MiniClaw-vs-myclaw.net-v2.md)

---

**最后更新**: 2026-03-15
