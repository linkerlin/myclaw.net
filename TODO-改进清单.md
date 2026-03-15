# MyClaw.NET 改进清单

> 基于 MiniClaw 深度对比后的具体行动项

---

## ✅ 已完成

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
| **项目类型检测** | `ProjectTypeDetector.cs` | React/Vue/Node/Go/Python/DotNet/Rust/Java ✅ |
| **上下文格式优化** | `WorkspaceInfo.cs` | `ToCompactContextString()` 与 MiniClaw 对齐 ✅ |
| **AI CLI 检测** | `AiCliDetector.cs` | Claude/Gemini/Kimi/Aider 检测 ✅ |

---

## 🔨 待办事项

### 🔴 高优先级 (当前)

#### 1. 一键安装脚本 ✅ 已完成
```bash
# ✅ 已创建: scripts/install.sh
# ✅ 已创建: scripts/install.ps1
# 功能:
#   - 自动检测平台 (Linux/macOS/Windows)
#   - 自动检测架构 (x64/arm64/arm)
#   - 彩色输出和错误处理
#   - 支持自定义版本和安装目录
#   - PATH 检查和提示
```

### 🟡 中优先级 (下一阶段)

#### 3. GitHub Actions 自动发布 ✅ 已完成
```yaml
# ✅ 已创建: .github/workflows/release.yml
# 功能:
#   - 6 个平台构建 (linux-x64/linux-arm64/win-x64/win-arm64/osx-x64/osx-arm64)
#   - 自动生成 Release Notes
#   - 提供一键安装命令
#   - 可选: 自动发布到 NuGet
# 触发方式:
#   - 推送 v* 标签自动触发
#   - 或手动触发 (workflow_dispatch)
```

#### 4. 技能缓存 (5s TTL) ✅ 已完成
```csharp
// ✅ 已修改: src/MyClaw.Skills/SkillManager.cs
// 功能:
//   - 5秒 TTL 自动缓存
//   - 缓存统计 (hits/misses/hit rate)
//   - 自动加载和过期检测
//   - ClearCache() 和 ResetStats() 方法
// 测试: 14 个单元测试全部通过
```

#### 5. 向量记忆持久化 ✅ 已完成
```csharp
// ✅ 已创建: src/MyClaw.Core/VectorMemory/PersistentVectorStore.cs
// 功能:
//   - 自动保存 (可配置间隔，默认5分钟)
//   - GZip 压缩 (减少存储空间)
//   - 脏数据追踪 (只保存变更)
//   - 备份机制 (保存失败自动恢复)
//   - 统计信息 (条目数、大小、命中率)
//   - 线程安全 (ConcurrentDictionary)
// 测试: 19 个单元测试全部通过
```

#### 6. 向量记忆优化 (低优先级)
- [ ] 记忆压缩和去重
- [ ] 跨会话记忆共享
- [ ] 向量索引优化 (HNSW/IVF)

### 🟢 低优先级 (可选/未来)

- [ ] 进化算法语义分析增强
- [ ] 文件健康检查 (DNA更新频率)
- [ ] 心跳自主执行完整实现 (AiCliDetector ✅, AutonomousExecutor ⏳)
- [ ] MCP 工具发现协议支持
- [ ] 多模态记忆 (图像/音频)

> **注意**: Telegram/飞书等多平台渠道支持已**从计划中移除**，项目专注于核心 MCP 功能。

---

## 📊 进度追踪

| 任务 | 状态 | 优先级 | 预计工时 | 负责人 |
|------|------|--------|----------|--------|
| ~~项目类型检测~~ | ✅ 已完成 | 🔴 高 | 4h | - |
| ~~上下文格式优化~~ | ✅ 已完成 | 🔴 高 | 2h | - |
| ~~AI CLI检测~~ | ✅ 已完成 | 🟡 中 | 4h | - |
| ~~一键安装脚本~~ | ✅ 已完成 | 🔴 高 | 4h | - |
| ~~GitHub Actions发布~~ | ✅ 已完成 | 🟡 中 | 4h | - |
| ~~技能缓存~~ | ✅ 已完成 | 🟡 中 | 2h | - |
| ~~向量记忆持久化~~ | ✅ 已完成 | 🟡 中 | 6h | - |

---

## 🎯 验收标准

### 项目类型检测 ✅
- [x] 正确识别 React/Vue/Node/Go/Python/DotNet/Rust/Java/Docker/Angular/Svelte/Ruby/PHP 项目
- [x] 置信度评分机制
- [x] 集成到 `WorkspaceInfo.ToContextString()` 和 `ToCompactContextString()`

### 上下文格式优化 ✅
- [x] 输出包含: 项目名称、路径、Git分支、技术栈
- [x] 紧凑格式: `Project: X | Path: Y` \n `Git: Z | dirty (+N files)` \n `Stack: A, B`
- [x] 脏状态显示: `dirty (+N files)`

### AI CLI 检测 ✅
- [x] 检测 Claude/Gemini/Kimi/Aider CLI
- [x] 获取版本信息和路径
- [x] 异步检测，支持 CancellationToken

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
