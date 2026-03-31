# MyClaw.NET TODO - 当前实施清单 v5.1

> 更新日期: 2026-03-31
> 工作原则: 先打通，后扩展；先事实，后叙事
> 说明: 本清单与《改进方案和计划.md》保持同一口径

---

## 使用说明

- `[x]` 已进入主路径并可用
- `[ ]` 仍需完成
- “已实现但待接线”表示代码存在，但用户主流程尚未真正受益

---

## ✅ 已进入主路径

- [x] `myclaw_update` 已集成 `TelomereGuard` + `PurposeMap`
- [x] `SignalDetector`、`EvolutionEngine`、`MethylationManager` 已形成基础进化链路
- [x] `AnalyticsService` 与 `ToolUsageTracker` 已接入 MCP 工具调用统计
- [x] `DailyBriefingService` 已通过 `myclaw_briefing` 暴露
- [x] `HeartbeatService` + `AiCliDetector` + `AutonomousExecutor` 已具备 AI CLI 自主执行与 fallback
- [x] 统一 prompt 装配已接入 Agent / FallbackAgent，包含 workspace / time mode / continuation / affect / nociception / methylation / memory
- [x] `ContextCompiler` 已支持结构化 skeletonization，超预算时优先保留 frontmatter / 标题 / 尾部上下文
- [x] `BoredomEngine` 已接入 Heartbeat / Gateway 编排，并具备冷却控制
- [x] `MyceliumNetwork` 已接入心跳吸收与 MCP 更新分泌路径
- [x] `RibosomePruner` 已改为使用真实 `bootCount` 与 `toolCalls`
- [x] `EntityStore` 已支持 `Vitality` 日衰减、提及时恢复与归零自动清理
- [x] Skills 已统一为 `skills/<name>/SKILL.md` 目录结构，并支持 `onBoot` / `onHeartbeat` / `onMemoryWrite` / `onFileChanged` hooks
- [x] `PerceptionProvider` 抽象已建立，并通过 `PerceptionContextService` 接入统一 prompt 主路径
- [x] Windows perception provider 已通过 Win32 API 提供前台应用、通知状态与电池信息，并通过 `PriorityHint` 接入 ACE
- [x] `SubstrateHarvester` 已通过 `myclaw_skill harvest` / `skills harvest` 扫描 Copilot / Cursor / Claude / Windsurf 常见规则并转换为内部 skills
- [x] `myclaw_mutate` 已进入 MCP 主路径，限制 `SOUL.md` / `IDENTITY.md` 重写并带备份与 purpose 返回
- [x] `myclaw_reproduce` 已进入 MCP 主路径，可生成包含核心 DNA / skills / entities / manifest 的 `.spore.zip`
- [x] 安装脚本、构建脚本、发布目录已存在
- [x] 向量记忆 / RAG 已实现

---

## ⚠️ 已实现但待接线

- 当前无阻塞级待接线项；后续重点转向 P1 能力补完

---

## 🔴 P0 - 当前必须完成

### P0-1: 统一上下文装配主路径

**目标**: 用统一服务替代 `MyClawAgent.BuildSystemPrompt()` 的手工拼接

- [ ] 设计 `AgentContextService` 或等价装配层
- [ ] 聚合 workspace / time mode / continuation / affect / methylation / nociception / memory
- [ ] 在 Agent 和 Gateway 共用同一装配逻辑
- [ ] 用 `ContextCompiler` 控制预算与截断

当前进展:
- [x] 已由 `AgentPromptContextBuilder` 完成主路径装配
- [x] Agent / FallbackAgent 已共用同一装配逻辑
- [x] `ContextCompiler` 已进入主路径

**验收标准**:
- [x] 主提示词包含工作区信息
- [x] 主提示词包含时间模式或返回场景摘要
- [x] 不再直接手工拼接多个 DNA 文件作为最终 prompt

### P0-2: 运行时编排打通

**目标**: 让已存在模块真正参与系统行为

- [ ] 为 `BoredomEngine` 增加明确触发入口
- [ ] 为 `MyceliumNetwork` 增加吸收入口
- [ ] 在痛觉 / 工具经验写入后增加分泌入口
- [ ] 为 Daily Briefing 设定触发时机或 feature flag

当前进展:
- [x] `BoredomEngine` 已由 Heartbeat / Gateway 触发
- [x] `MyceliumNetwork` 已增加心跳吸收入口
- [x] MCP 的 `TOOLS.md` / `NOCICEPTION.md` 更新已触发分泌
- [x] heartbeat supplemental context 默认改为低噪音摘要，详细列表仅在 verbose 模式展开

**验收标准**:
- [x] 至少两个“待接线模块”进入主流程
- [x] 行为有日志可观察
- [x] 不引入默认高频噪音

### P0-3: 修正器官退化的数据来源

**目标**: 防止 `RibosomePruner` 基于错误统计做决定

- [ ] 用真实启动次数替代 `TotalEvolutions`
- [ ] 用真实工具调用统计替代空字典
- [ ] 为误删风险加保护测试

当前进展:
- [x] 已接入 `AnalyticsService.BootCount`
- [x] 已接入 `AnalyticsService.ToolCalls`
- [x] 已补器官退化保护测试

**验收标准**:
- [x] 50 次启动前不修剪
- [x] 已使用工具不被误删
- [x] 日志与实际修剪结果一致

### P0-4: 文档事实口径治理

**目标**: 让计划文档重新可信

- [x] 更新 `DOCUMENTS-INDEX.md` 失效引用
- [x] 标记过时的方案 / 进度文档
- [x] 以当前计划和 TODO 作为近期单一事实口径

当前进展:
- [x] 已更新 `DOCUMENTS-INDEX.md`、`README.md`、`AGENTS.md` 的失效引用
- [x] 已为 `实施总结报告.md`、`实施进度报告-v2.md`、`实施计划.md` 增加历史说明
- [x] 当前文档索引已明确以本计划和 TODO 作为近期事实口径

**验收标准**:
- [x] 索引不再指向已删除文档
- [x] 已实现 / 待接线 / 缺失 三种状态分明
- [x] 新人不需要翻多份历史文档才能理解当前状态

### P0-5: 关键集成测试

**目标**: 优先验证接线，而不是只验证单模块

- [x] 为统一上下文装配增加集成测试
- [x] 为 heartbeat 的 CLI / fallback 增加测试
- [x] 为 `myclaw_update` 的 guard + purpose 返回增加测试
- [x] 为器官退化真实统计输入增加测试

当前进展:
- [x] 已为 `MyClawAgent` / `FallbackAgent` 增加统一上下文装配端到端测试
- [x] 已为器官退化真实统计输入增加测试
- [x] 已为 heartbeat 的 CLI / fallback / supplemental context 增加测试
- [x] 已为 `myclaw_update` 增加 guard 拒绝与 purpose 回传测试

**验收标准**:
- [x] 新增测试覆盖 P0 主路径
- [x] 至少覆盖成功路径与一个失败 / 回退路径

---

## 🟡 P1 - 核心能力补完

### P1-1: Token 骨架化
- [x] 为 `ContextCompiler` 增加结构化 skeletonization
- [x] 优先保留 frontmatter / 标题 / 尾部上下文
- [x] 替代当前简单截断策略

当前进展:
- [x] 超预算段落已改为按 frontmatter / 标题 / 尾部上下文生成 skeleton
- [x] 已补 `ContextCompiler` 单元测试覆盖结构保留与优先级行为
- [x] 已回归 `AgentPromptContextBuilder` 测试，确认主路径 prompt 未被破坏

验收标准:
- [x] 超预算时不再直接做 `Substring` 粗截断
- [x] 压缩结果保留关键结构信息与尾部上下文
- [x] 既有 prompt 装配测试继续通过

### P1-2: 实体 Apoptosis
- [x] 为实体增加 `Vitality`
- [x] 增加每日衰减与提及时恢复
- [x] 活力归零时自动清理

当前进展:
- [x] `Entity` 已增加 `Vitality` 与 `VitalityUpdatedAt`
- [x] `EntityStore` 已在读取路径上执行惰性日衰减，并在再次提及时恢复活力
- [x] 活力归零的实体会在 store 刷新时自动移除
- [x] `myclaw_entity` 与 Daily Briefing 的实体摘要已显示 vitality，生命周期状态可直接观察

验收标准:
- [x] 新实体默认带初始活力
- [x] 跨天访问会触发衰减，重复提及会恢复活力
- [x] 活力降为 0 后不会继续出现在查询结果中

### P1-3: `myclaw_mutate`
- [x] 实现受限 DNA 重写
- [x] 接入 `TelomereGuard`
- [x] 自动备份与记录

当前进展:
- [x] 已限制仅允许 `SOUL.md` / `IDENTITY.md`
- [x] 已在写入前执行 `TelomereGuard`
- [x] 已生成 `.bak` 备份并返回 `PurposeMap` 说明

验收标准:
- [x] 非白名单 DNA 文件会被拒绝
- [x] 非法结构写入会被拒绝
- [x] 成功写入会保留备份并更新内容

### P1-4: `myclaw_reproduce`
- [x] 打包核心 DNA / skills / entities
- [x] 优先支持 zip
- [x] 与模板和永生工具名单保持一致

当前进展:
- [x] 已生成 `.spore.zip` 并打包核心 DNA / `memory/MEMORY.md` / `entities.json` / `skills/` / `manifest.json`
- [x] 默认 `RIBOSOME` 与模板 `RIBOSOME.json` 已同步暴露 `myclaw_reproduce`
- [x] 已增加 MCP tools/list 与压缩包内容集成测试

验收标准:
- [x] zip 包包含核心 DNA 与 `manifest.json`
- [x] 工具在 tools/list 可见
- [x] 模板与运行时定义一致

### P1-5: 强化 Dream
- [x] 读取最近日志 / 统计
- [x] 生成可行动建议
- [x] 第一阶段仅 dry-run，不直接写 DNA

### P1-6: Skill Hooks
- [x] 扩展 skill frontmatter
- [x] 支持 `onBoot`
- [x] 支持 `onHeartbeat`
- [x] 支持 `onMemoryWrite`
- [x] 支持 `onFileChanged`

当前进展:
- [x] `SkillLoader` / `SkillManager` 已支持 hooks 与 `filePatterns` frontmatter 解析和匹配
- [x] Gateway / Agent / CLI / MCP 已统一使用 `skills/<name>/SKILL.md` 目录结构
- [x] `onBoot` 已注入 Agent system prompt，`onHeartbeat` 已注入 heartbeat supplemental context
- [x] `onMemoryWrite` / `onFileChanged` 已通过 MemoryTool 和 MCP 写操作结果回传 hook 上下文

验收标准:
- [x] myclaw_skill create 产出的文件可被 loader 直接加载
- [x] boot hook 会进入系统提示词
- [x] memory / file change hook 能在现有主路径中真实触发

---

## 🟢 P2 - 平台与生态扩展

### P2-1: 平台感知抽象
- [x] 设计 `PerceptionProvider` 接口
- [x] 把平台差异从业务逻辑中抽离

当前进展:
- [x] 已新增 `IPerceptionProvider`、默认 provider 工厂与 `PerceptionSnapshot`
- [x] 已通过 `PerceptionContextService` 将平台感知统一格式化为 `## PERCEPTION` 段落
- [x] `AgentPromptContextBuilder` 主路径已接入 perception context，端到端 prompt 测试已覆盖

验收标准:
- [x] 业务层不再直接依赖未来的平台细节实现
- [x] 平台感知可通过统一上下文服务进入主提示词
- [x] 已有单元测试与端到端抓包测试验证接线

### P2-2: macOS / Windows 感知
- [ ] macOS: DND / battery / active apps
- [x] Windows: 能力对等或降级实现
- [x] 接入 ACE 上下文优先级

当前进展:
- [x] Windows provider 已通过 Win32 API 读取 notification state、battery status 与 foreground app
- [x] `PerceptionSnapshot.PriorityHint` 已进入 `PerceptionContextService`，平台感知上下文可参与 ACE 优先级排序
- [x] macOS provider 已改为 `defaults` / `pmset` / `system_profiler` / `osascript` 多探针实现，并补 provider 单测覆盖 DND / battery / frontmost app 的解析与降级路径

验收标准:
- [ ] macOS 原生感知信号稳定可用（待 macOS 宿主实机验证）
- [x] Windows 至少有可用的降级或对等实现
- [x] 感知结果会影响进入 prompt 的上下文优先级

### P2-3: Substrate Harvesting
- [x] 扫描外部 AI 工具配置与规则
- [x] 转换为内部 skills
- [x] 明确导入边界与安全策略

当前进展:
- [x] 已支持扫描 GitHub Copilot、Cursor、Claude、Windsurf 的常见规则文件并映射为候选 skills
- [x] 已通过 `myclaw_skill harvest` 与 `skills harvest` 暴露 dry-run / apply 导入路径
- [x] 默认只扫描白名单文本路径，限制单文件大小，跳过 reparse point / 二进制内容，且不自动启用 hooks、不默认覆盖已有 skills

验收标准:
- [x] dry-run 能列出候选导入项与边界策略
- [x] apply 能写入 `skills/<name>/SKILL.md`
- [x] 导入策略对覆盖和扫描边界有明确保护

### P2-4: 文档扩张
- [ ] 在事实稳定后再规划章节教程
- [ ] 避免继续生成重复的计划文档
- [x] 优先补架构图、主路径说明、模块编排图

当前进展:
- [x] 已新增 `docs/主路径与模块编排.md`，集中说明 Agent / Heartbeat / MCP 三条主路径与共享模块
- [x] 已把该文档加入 `DOCUMENTS-INDEX.md` 与 `README.md` 的当前导航
- [x] 已新增 `docs/Heartbeat编排与自主执行.md`，单独展开 Heartbeat 的触发、补充上下文、自主执行与 fallback

---

## 🚫 暂不纳入当前冲刺

以下事项保留在远期，不再挤占 P0：

- [ ] 大规模章节教程写作
- [ ] 只对单一平台有价值的感知特性
- [ ] 新增更多平行的生物学隐喻模块
- [ ] 在主路径未统一前继续扩张工具种类

---

## 📊 当前统计

| 类别 | 数量 | 说明 |
|:-----|:----:|:-----|
| 已进入主路径 | 19 | 已能被用户主流程直接感知 |
| 已实现但待接线 | 0 | 当前无阻塞级待接线项 |
| P0 任务 | 5 | 当前必须完成 |
| P1 任务 | 6 | 主路径稳定后的补完项 |
| P2 任务 | 4 | 平台与生态扩展 |

---

**最后更新**: 2026-03-31
**下次审查**: 在 macOS 宿主验证 P2-2 感知信号，或继续细化 P2-4 的专题文档
**当前原则**: 先打通，后扩展
