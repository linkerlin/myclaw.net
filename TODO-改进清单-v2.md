# MyClaw.NET 改进清单 v2.0

> 基于 MiniClaw 最近两周 (3 月 9 日 -3 月 23 日) 进化深度对比后的具体行动项
>
> **分析周期**: 2026 年 3 月 9 日 - 3 月 23 日
> **对比版本**: MiniClaw v0.9.5 vs myclaw.net v0.8.0
> **最后更新**: 2026-03-23
> **参考文档**: `改进方案和计划-v4.md`

---

## ✅ 已完成

| 模块 | 文件 | 说明 |
|------|------|------|
| 工作区检测 | `WorkspaceDetector.cs` | Git 状态、技术栈检测 ✅ |
| 技术栈识别 | `TechStackDetector.cs` | 50+ 技术 ✅ |
| 使用统计 | `AnalyticsService.cs` | 工具/启动/技能统计 ✅ |
| 每日简报 | `DailyBriefingService.cs` | 昨日回顾/统计/实体 ✅ |
| 上下文编译 | `ContextCompiler.cs` | Token 预算/优先级 ✅ |
| 情感系统 | `AffectManager.cs` | 4 维模型 ✅ |
| 进化引擎 | `EvolutionEngine.cs` | 模式检测/DNA 更新 ✅ |
| 痛觉记忆 | `NociceptionManager.cs` | 指数衰减 ✅ |
| 好奇心引擎 | `CuriosityEngine.cs` | 6 种目标 ✅ |
| 向量记忆 | `VectorMemory/` | RAG 检索 ✅ |
| **项目类型检测** | `ProjectTypeDetector.cs` | React/Vue/Node/Go/Python/DotNet/Rust/Java ✅ |
| **上下文格式优化** | `WorkspaceInfo.cs` | `ToCompactContextString()` 与 MiniClaw 对齐 ✅ |
| **AI CLI 检测** | `AiCliDetector.cs` | Claude/Gemini/Kimi/Aider 检测 ✅ |
| **一键安装脚本** | `scripts/install.sh`, `scripts/install.ps1` | 跨平台安装 ✅ |
| **GitHub Actions 发布** | `.github/workflows/release.yml` | 6 平台构建 ✅ |
| **技能缓存** | `SkillManager.cs` | 5s TTL ✅ |
| **向量记忆持久化** | `PersistentVectorStore.cs` | GZip 压缩 + 自动保存 ✅ |
| **三层免疫系统** | `PurposeMap.cs` + `TelomereGuard.cs` | 端粒守卫 + PURPOSE_MAP 自检 ✅ |
| **器官退化** | `RibosomePruner.cs` | 50 次心跳后剔除未使用工具 ✅ |
| **无聊引擎** | `BoredomEngine.cs` | 30 分钟无活动扫描代码库 ✅ |
| **菌丝共生网络** | `MyceliumNetwork.cs` | 跨实例知识共享 ✅ |

---

## 🔨 待办事项

> **注意**: 以下待办事项中，第 1-4 项（三层免疫系统、器官退化、无聊引擎、菌丝共生网络）已于 2026-03-23 完成实现。查看 [实施总结报告](./实施总结报告.md) 了解详情。

### 🔴 高优先级 (基于 MiniClaw v0.9.5 对比)

#### 1. 三层免疫系统 (✅ 已完成)

**状态**: ✅ 已完成 (2026-03-23)

**实施文件**:
- [x] `src/MyClaw.Core/Dna/PurposeMap.cs` - PURPOSE_MAP 自检镜像
- [x] `src/MyClaw.Core/Dna/TelomereGuard.cs` - 端粒守卫
- [x] `templates/AGENTS.md` - 添加"AI 的思考逻辑"列
- [x] `src/MyClaw.MCP/McpServer.cs` - 集成端粒守卫和 PURPOSE_MAP

**验收标准**:
- [x] DNA 写入前自动校验结构完整性
- [x] 写入后返回文件职责声明
- [x] 信号检测表包含完整的思考逻辑

---

#### 2. 器官退化 (✅ 已完成)

**状态**: ✅ 已完成 (2026-03-23)

**实施文件**:
- [x] `src/MyClaw.Core/Ribosome/RibosomePruner.cs` - 核糖体修剪器
- [x] `src/MyClaw.Core/Evolution/EvolutionEngine.cs` - 集成修剪器

**验收标准**:
- [x] 50 次心跳后检查工具使用频率
- [x] 从未使用的工具自动剔除 (永生工具除外)
- [x] 退化日志记录到 HEARTBEAT.md

---

### 🟡 中优先级

#### 3. 无聊引擎 (✅ 已完成)

**状态**: ✅ 已完成 (2026-03-23)

**实施文件**:
- [x] `src/MyClaw.Core/Curiosity/BoredomEngine.cs` - 无聊引擎

**验收标准**:
- [x] 30 分钟无活动触发
- [x] 随机扫描源码文件
- [x] 提取 TODO/FIXME 写入 HORIZONS.md
- [x] 2 小时内不重复执行

---

#### 4. 菌丝共生网络 (✅ 已完成)

**状态**: ✅ 已完成 (2026-03-23)

**实施文件**:
- [x] `src/MyClaw.Core/Mycelium/MyceliumNetwork.cs` - 菌丝网络

**验收标准**:
- [x] 分泌孢子 (TOOLS/NOCICEPTION 更新时)
- [x] 吸收异体孢子
- [x] 消耗已吸收孢子
- [x] 跨实例群体免疫

---

#### 5. 表观遗传甲基化增强 (部分实现)

**状态**: ⚠️ 部分实现

**差距**: MiniClaw 稳定性动态计算 + SOUL.md 注释完整，myclaw.net 实现不完善

**实施文件**:
- [ ] `src/MyClaw.Core/Epigenetics/MethylationManager.cs` (修改) - 稳定性动态计算
- [ ] `templates/SOUL.md` (修改) - [METHYLATED] 注释格式

**验收标准**:
- [ ] 稳定性随重复次数增加：`calculateStability(repeatCount)`
- [ ] SOUL.md 使用 `[METHYLATED]` 标记
- [ ] 48 小时冷却期

**预计工时**: 4 小时
**优先级**: 🟡🟢

---

#### 6. 潜意识嗅探器 (完全缺失)

**状态**: ❌ 未开始

**差距**: MiniClaw 文件系统监听检测用户挣扎，myclaw.net 无此机制

**实施文件**:
- [ ] `src/MyClaw.Core/Workspace/SubconsciousWatcher.cs` (新增)

**验收标准**:
- [ ] 配置文件频繁修改检测 (>=4 次)
- [ ] 大规模重构检测 (>=50 次变更)
- [ ] 原生通知提醒 (macOS/Windows)

**预计工时**: 6 小时
**优先级**: 🟡🟡

---

#### 7. 进化日志 (完全缺失)

**状态**: ❌ 未开始

**差距**: MiniClaw 记录完整进化历史，myclaw.net 无进化日志

**实施文件**:
- [ ] `src/MyClaw.Core/Evolution/EvolutionLogger.cs` (新增)
- [ ] `memory/YYYY-MM-DD.md` (新增目录结构)

**验收标准**:
- [ ] 每次进化记录到 memory/日期.md
- [ ] 包含进化类型、置信度、应用突变

**预计工时**: 2 小时
**优先级**: 🟢🟢

---

### 🟢 低优先级 (可选/未来)

#### 8. 孢子繁殖协议 (完全缺失)

**状态**: ❌ 未开始

**差距**: MiniClaw 可吐出带有性格和记忆烙印的 .spore 快照包

**实施文件**:
- [ ] `src/MyClaw.Skills/Reproduce.cs` (新增) - myclaw_reproduce 工具

**验收标准**:
- [ ] 打包非易失性遗传物质 (SOUL.md, IDENTITY.md 等)
- [ ] 生成 .spore 快照文件
- [ ] 支持跨实例移植

**预计工时**: 4 小时
**优先级**: 🟢🟡

---

#### 9. 基因突变工具 (完全缺失)

**状态**: ❌ 未开始

**差距**: MiniClaw 可通过 miniclaw_mutate 主动重写 SOUL.md/IDENTITY.md

**实施文件**:
- [ ] `src/MyClaw.Skills/Mutate.cs` (新增) - myclaw_mutate 工具

**验收标准**:
- [ ] 仅允许修改 SOUL.md 和 IDENTITY.md
- [ ] 写入前端粒守卫校验
- [ ] 自动备份

**预计工时**: 3 小时
**优先级**: 🟢🟡

---

#### 10. 文档体系建设 (严重不足)

**状态**: ⚠️ 部分实现

**差距**: MiniClaw 20 章教程 + 5 篇专题论文 (~50,000 字)，myclaw.net 仅 3 份改进文档

**实施计划**:

**章节式教程** (20 章):
- [ ] 第一章：项目概述与理念
- [ ] 第二章：架构设计
- [ ] 第三章：DNA 模板系统
- [ ] 第四章：自适应上下文引擎
- [ ] 第五章：记忆系统
- [ ] 第六章：情感与认知系统
- [ ] 第七章：进化引擎
- [ ] 第八章：技能系统
- [ ] 第九章：三层免疫系统
- [ ] 第十章：菌丝共生网络
- [ ] 第十一章 - 第二十章...

**专题论文** (5 篇):
- [ ] 三层免疫系统架构
- [ ] 表观遗传表达
- [ ] 端粒守卫
- [ ] 菌丝共生网络
- [ ] 数字生命体设计哲学

**预计工时**: 40 小时  
**优先级**: 🔴🔴 (重要但复杂)

---

#### 11. 进化算法语义分析增强 (部分实现)

**差距**: MiniClaw 语义聚类分析 + 长期积累，myclaw.net 简单正则匹配

**实施文件**:
- [ ] `src/MyClaw.Core/Evolution/EvolutionEngine.cs` (修改)
- [ ] `src/MyClaw.Core/Evolution/SemanticClustering.cs` (新增)

**验收标准**:
- [ ] 日志窗口扩展到 30 天
- [ ] 语义聚类分析
- [ ] 基于语义指纹的模式合并
- [ ] 置信度演化

**预计工时**: 8 小时  
**优先级**: 🟡🟡

---

#### 12. 文件健康检查 (完全缺失)

**差距**: MiniClaw 监控 DNA 文件更新频率，myclaw.net 无此机制

**实施文件**:
- [ ] `src/MyClaw.Core/Dna/DnaHealthMonitor.cs` (新增)

**验收标准**:
- [ ] 监控 DNA 文件更新频率
- [ ] 异常更新告警
- [ ] 健康报告

**预计工时**: 3 小时  
**优先级**: 🟢🟢

---

## 📊 进度追踪

| 任务 | 状态 | 优先级 | 预计工时 | 实际工时 | 负责人 |
|------|------|--------|----------|----------|--------|
| 三层免疫系统 | ✅ 已完成 | 🔴 高 | 6h | ~4h | - |
| 器官退化 | ✅ 已完成 | 🔴 中 | 3h | ~3h | - |
| 无聊引擎 | ✅ 已完成 | 🟡 中 | 4h | ~4h | - |
| 菌丝共生网络 | ✅ 已完成 | 🟡 中 | 8h | ~6h | - |
| 表观遗传增强 | ⏳ 待实施 | 🟡 中 | 4h | - | - |
| 潜意识嗅探器 | ⏳ 待实施 | 🟡 中 | 6h | - | - |
| 进化日志 | ⏳ 待实施 | 🟢 低 | 2h | - | - |
| 孢子繁殖 | ⏳ 待实施 | 🟢 低 | 4h | - | - |
| 基因突变工具 | ⏳ 待实施 | 🟢 低 | 3h | - | - |
| 文档体系建设 | ⏳ 待实施 | 🔴 高 | 40h | - | - |
| 语义分析增强 | ⏳ 待实施 | 🟡 中 | 8h | - | - |
| 文件健康检查 | ⏳ 待实施 | 🟢 低 | 3h | - | - |

**完成进度**: 4/12 核心任务已完成 (33%)

---

## 📚 参考文档

- [改进方案和计划-v4.md](./改进方案和计划-v4.md) - 完整对比分析和实施计划
- [改进方案和计划-v3-深度对比.md](./改进方案和计划-v3-深度对比.md) - 上一版本对比
- [MiniClaw-vs-myclaw.net-v2.md](./docs/MiniClaw-vs-myclaw.net-v2.md) - 功能对照表
- [MiniClaw 仓库](https://github.com/8421bit/MiniClaw) - 参考实现

---

**最后更新**: 2026-03-23  
**版本**: v2.0
