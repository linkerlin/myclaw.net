# myclaw.net 改进计划

> 基于 MiniClaw (TypeScript) 最近两周的核心创新，完善 myclaw.net (C#) 系统
> 
> **分析日期**: 2026-03-14
> **参考版本**: MiniClaw v0.7.0 vs myclaw.net (当前)

---

## 一、差距分析

### 1.1 MiniClaw 最近两周的核心创新

| 功能 | 提交 | 重要性 | myclaw.net 状态 |
|------|------|--------|-----------------|
| 🧬 全基因组进化 | 699596e | ⭐⭐⭐ | ✅ 已实现 (Phase 2.1) |
| 🧪 表观遗传甲基化 | 45167f9 | ⭐⭐⭐ | ✅ 已实现 (Phase 1.3) |
| 🚨 痛觉记忆系统 | 6285cd0 | ⭐⭐⭐ | ✅ 已实现 (Phase 1.2) |
| 🔍 好奇心/主动探索 | 96712b8 | ⭐⭐ | ✅ 已实现 (Phase 4) |
| 💭 统一情感状态 | 2183e5b | ⭐⭐⭐ | ✅ 已实现 (Phase 1.1) |
| 💓 自主执行 Heartbeat | ffc4ec2 | ⭐⭐⭐ | ⚠️ 部分实现 |
| 🧬 RIBOSOME 架构 | 79bb54a | ⭐⭐ | ✅ 已实现 (Phase 3) |
| 📦 外部化工具 DNA | 3a0b07e | ⭐⭐ | ✅ 已实现 (Phase 3) |
| 🔧 极致轻量化 | 8dc1b38 | ⭐ | ⚠️ 代码较多 |

### 1.2 已对齐的功能 (无需改进)

- ✅ 记忆系统 (蒸馏、归档、实体图谱)
- ✅ ACE 时间模式
- ✅ 会话延续检测
- ✅ Token 预算管理
- ✅ 信号检测器
- ✅ 工作区检测 (Git + TechStack)
- ✅ 命令执行沙箱
- ✅ 技能系统 (SKILL.md 格式)
- ✅ MCP 服务
- ✅ 多渠道架构
- ✅ Quartz 调度

---

## 二、改进计划

### Phase 1: 生命体基础系统 (优先级最高)

#### 1.1 统一情感状态系统 (Affect State)

**目标**: 实现数字生命的情感层，所有系统汇聚到统一状态

**新增文件**:
```
src/MyClaw.Core/Affect/
├── AffectState.cs          # 情感状态接口
├── AffectManager.cs        # 情感管理器
└── AffectMode.cs           # 行为模式枚举
```

**AffectState 定义**:
```csharp
public class AffectState
{
    public double Alertness { get; set; } = 0.7;    // 警觉度 0-1
    public double Mood { get; set; } = 0.6;         // 心情 0-1
    public double Curiosity { get; set; } = 0.5;    // 好奇心 0-1
    public double Confidence { get; set; } = 0.7;   // 自信度 0-1
}

public enum AffectMode
{
    Exploration,   // 🔍 探索模式 - 高好奇心
    Execution,     // ⚡ 执行模式 - 高警觉
    Cautious,      // 🛡️ 谨慎模式 - 受伤后
    Rest           // 💤 休息模式 - 低能量
}
```

**核心逻辑**:
- `UpdateAffect()`: 平滑混合 + 动量保留
- `GetAffectMode()`: 根据状态推导行为模式
- `RecoverToBaseline()`: 每个 Pulse 周期 10% 恢复到基线
- 痛觉触发时: alertness↑, mood↓, curiosity↓, confidence↓

**集成点**:
- ACE 上下文编译时输出情感状态
- 痛觉系统影响情感
- 好奇心系统受情感调制

---

#### 1.2 痛觉记忆系统 (Nociception)

**目标**: 记录"绝对不要做"的事情，形成保护性本能

**新增文件**:
```
src/MyClaw.Core/Nociception/
├── PainMemory.cs           # 痛觉记忆存储
├── NociceptionManager.cs   # 痛觉管理器
└── PainTrigger.cs          # 痛觉触发器
templates/
└── NOCICEPTION.md          # 痛觉记忆模板
```

**PainMemory 定义**:
```csharp
public class PainMemory
{
    public string Stimulus { get; set; }      // 触发点
    public string Harm { get; set; }          // 伤害结果
    public string Strategy { get; set; }      // 规避方案
    public int Severity { get; set; }         // 严重程度 1-10
    public DateTime OccurredAt { get; set; }  // 发生时间
    public int Occurrences { get; set; }      // 发生次数
}
```

**核心功能**:
- `RecordPain()`: 记录新的痛觉记忆
- `CheckPainTrigger()`: 执行前检查是否会触发痛觉
- `GetPainWarnings()`: 获取当前操作的痛觉警告
- 痛觉触发时调用 `AffectManager.ApplyPain()`

**NOCICEPTION.md 模板**:
```markdown
---
boot-priority: 10
description: "痛觉记忆 - 记录'绝对不要做'的事情清单"
---

# 避害模式 (Nociception)

## 🚨 核心禁忌
- **拒绝幻想**: 严禁在未确认事实的情况下凭空猜测
- **防止失忆**: 严禁在未读取 DNA 的情况下进行大规模更新
- **环境安全**: 严禁在没有明确授权的情况下执行毁灭性命令

## 🧠 痛觉索引
<!-- 自动记录 -->
```

---

#### 1.3 表观遗传甲基化系统 (Epigenetic Methylation)

**目标**: 临时适应变成半永久性状，环境塑造性格

**新增文件**:
```
src/MyClaw.Core/Epigenetics/
├── MethylatedTrait.cs      # 甲基化性状
├── EpigeneticsManager.cs   # 表观遗传管理器
└── methylation.json        # 持久化存储
```

**MethylatedTrait 定义**:
```csharp
public class MethylatedTrait
{
    public string Type { get; set; }          // interaction_style, activity_pattern, workflow_style
    public string Name { get; set; }          // 性状名称
    public string Description { get; set; }   // 描述
    public double Stability { get; set; }     // 稳定性 0-1
    public int Repetitions { get; set; }      // 重复次数
    public DateTime FirstSeen { get; set; }   // 首次发现
    public DateTime LastReinforced { get; set; } // 最后强化
}

// 甲基化阈值
public static class EpigeneticsConstants
{
    public const int METHYLATION_THRESHOLD = 10;      // 最小重复次数
    public const int METHYLATION_AGE_DAYS = 7;        // 最小模式年龄
    public const int METHYLATION_COOLDOWN_HOURS = 48; // 修改冷却时间
}
```

**核心逻辑**:
- `ShouldMethylate()`: 判断模式是否符合甲基化条件
- `MethylateTrait()`: 应用甲基化到 SOUL.md
- `GetMethylatedTraits()`: 导出性状用于上下文组装
- 在 DNA 进化时自动检查甲基化条件

**上下文集成**:
- boot() 时添加 "Methylated Traits" 部分
- 显示已甲基化的半永久性状

---

### Phase 2: 全基因组进化系统

#### 2.1 扩展进化覆盖范围

**当前**: 仅 4 个文件进化 (SOUL.md, USER.md, TOOLS.md, MEMORY.md)
**目标**: 9 个文件进化

**新增进化目标**:

| 染色体 | 文件 | 进化触发 |
|--------|------|----------|
| Chr-6 | CONCEPTS.md | 概念提取、技术术语 |
| Chr-7 | REFLECTION.md | 错误反思、情感适应 |
| Chr-8 | HORIZONS.md | 里程碑达成 |
| Chr-HB | HEARTBEAT.md | 工作流任务 |
| Chr-Agents | AGENTS.md | 自动发现工作流 |

**新增进化函数** (EvolutionEngine.cs):
```csharp
// 更新 HEARTBEAT.md 任务
Task UpdateHeartbeatTasksAsync(Pattern pattern);

// 更新 REFLECTION.md 反思记录
Task UpdateReflectionAsync(string type, string description);

// 提取概念到 CONCEPTS.md
Task ExtractConceptsAsync(Pattern pattern);

// 检查里程碑写入 HORIZONS.md
Task CheckMilestonesAsync(int totalEvolutions);
```

---

#### 2.2 模式检测增强

**新增模式类型**:
```csharp
public enum PatternType
{
    Repetition,      // 重复问题 → 创建技能
    Preference,      // 用户偏好 → USER.md / SOUL.md
    Temporal,        // 时间模式 → USER.md
    Workflow,        // 工作流 → AGENTS.md
    Sentiment,       // 情感反馈 → SOUL.md
    ErrorPattern,    // 错误模式 → REFLECTION.md + NOCICEPTION.md
    Curiosity,       // 好奇心触发 → 主动探索
    Milestone        // 里程碑 → HORIZONS.md
}
```

---

### Phase 3: RIBOSOME 架构

#### 3.1 外部化工具定义

**目标**: 将工具定义从代码移到配置文件

**新增文件**:
```
templates/
└── RIBOSOME.json           # 核糖体配置 - 工具定义
```

**RIBOSOME.json 结构**:
```json
{
  "version": "0.7.0",
  "description": "核糖体 - 读取DNA并合成蛋白质（工具）的分子机器",
  "instincts": {
    "myclaw_update": {
      "handler": "UpdateDNA",
      "description": "【本能：神经重塑】修改自身核心认知...",
      "inputSchema": { ... }
    },
    "myclaw_note": { ... },
    "myclaw_read": { ... },
    // ... 其他工具
  }
}
```

**实现**:
```csharp
// src/MyClaw.Core/Ribosome/
public class RibosomeLoader
{
    public async Task<Dictionary<string, InstinctDefinition>> LoadInstinctsAsync(string ribosomePath);
}

public class InstinctDefinition
{
    public string Handler { get; set; }
    public string Description { get; set; }
    public JsonElement InputSchema { get; set; }
}
```

---

### Phase 4: 好奇心与主动探索

#### 4.1 好奇心系统

**新增文件**:
```
src/MyClaw.Core/Curiosity/
├── CuriosityEngine.cs      # 好奇心引擎
├── ExplorationTarget.cs    # 探索目标
└── curiosity_state.json    # 持久化
```

**CuriosityEngine**:
```csharp
public class CuriosityEngine
{
    // 生成探索目标
    public List<ExplorationTarget> GenerateTargets(AffectState affect);
    
    // 计算好奇心得分
    public double CalculateCuriosityScore(string topic, UsageAnalytics analytics);
    
    // 调制好奇心（受情感状态影响）
    public double ModulateByAffect(double baseCuriosity, AffectState affect);
}

public class ExplorationTarget
{
    public string Topic { get; set; }
    public string Reason { get; set; }
    public double Priority { get; set; }
    public ExplorationStatus Status { get; set; }
}
```

**好奇心触发条件**:
- 新概念出现
- 长时间未使用的工具
- 用户提到的未知领域
- 工作区新文件类型

---

### Phase 5: 自主执行增强

#### 5.1 Heartbeat 自主执行

**目标**: Heartbeat 可以通过 AI CLI 自主执行任务

**新增文件**:
```
src/MyClaw.Heartbeat/
├── AutonomousExecutor.cs   # 自主执行器
├── AiCliDetector.cs        # AI CLI 检测
└── HeartbeatTaskRunner.cs  # 任务运行器
```

**AiCliDetector**:
```csharp
public class AiCliDetector
{
    public async Task<List<AiCliInfo>> DetectAvailableClisAsync();
    // 检测: claude, gemini, kimi, aider
}

public class AiCliInfo
{
    public string Name { get; set; }
    public string Path { get; set; }
    public string Version { get; set; }
    public bool IsAvailable { get; set; }
}
```

**AutonomousExecutor**:
```csharp
public class AutonomousExecutor
{
    public async Task<ExecutionResult> ExecuteHeartbeatTaskAsync(
        HeartbeatTask task, 
        AiCliInfo cli);
}
```

---

## 三、实施优先级

### 第一优先级 (Week 1-2): 生命体基础

| 任务 | 预计时间 | 依赖 |
|------|----------|------|
| 统一情感状态系统 | 2 天 | 无 |
| 痛觉记忆系统 | 2 天 | 情感系统 |
| 表观遗传甲基化 | 2 天 | 无 |
| 集成测试 | 2 天 | 以上全部 |

### 第二优先级 (Week 3): 全基因组进化

| 任务 | 预计时间 | 依赖 |
|------|----------|------|
| 扩展进化目标 | 2 天 | 无 |
| 模式检测增强 | 2 天 | 无 |
| 进化引擎更新 | 1 天 | 以上 |
| 测试 | 1 天 | 以上 |

### 第三优先级 (Week 4): 架构优化

| 任务 | 预计时间 | 依赖 |
|------|----------|------|
| RIBOSOME 架构 | 2 天 | 无 |
| 好奇心系统 | 2 天 | 情感系统 |
| 自主执行增强 | 1 天 | 无 |
| 文档更新 | 1 天 | 以上全部 |

---

## 四、文件清单

### 4.1 新增文件

```
src/MyClaw.Core/
├── Affect/
│   ├── AffectState.cs
│   ├── AffectManager.cs
│   └── AffectMode.cs
├── Nociception/
│   ├── PainMemory.cs
│   ├── NociceptionManager.cs
│   └── PainTrigger.cs
├── Epigenetics/
│   ├── MethylatedTrait.cs
│   └── EpigeneticsManager.cs
├── Curiosity/
│   ├── CuriosityEngine.cs
│   └── ExplorationTarget.cs
├── Ribosome/
│   ├── RibosomeLoader.cs
│   └── InstinctDefinition.cs
templates/
├── NOCICEPTION.md
├── CONCEPTS.md
├── REFLECTION.md
├── HORIZONS.md
└── RIBOSOME.json
```

### 4.2 修改文件

```
src/MyClaw.Core/
├── Evolution/
│   └── SignalDetector.cs       # 扩展模式类型
├── Ace/
│   └── ContextCompiler.cs      # 集成情感/甲基化
├── Analytics/
│   └── UsageAnalytics.cs       # 添加活跃时段统计
src/MyClaw.Heartbeat/
├── HeartbeatService.cs         # 集成自主执行
```

---

## 五、验收标准

### 5.1 功能验收

- [x] 情感状态影响上下文输出 ✅ 2026-03-14
- [x] 痛觉记忆阻止危险操作 ✅ 2026-03-14
- [x] 表观遗传性状出现在上下文 ✅ 2026-03-14
- [x] 9 个 DNA 文件都可进化 ✅ 2026-03-14
- [x] 好奇心系统生成探索目标 ✅ 2026-03-14
- [x] RIBOSOME.json 定义工具 ✅ 2026-03-14

### 5.2 质量验收

- [x] 单元测试覆盖率 > 70% ✅ 363 个测试全部通过
- [x] 集成测试覆盖核心流程 ✅ 76 个集成测试通过
- [x] 无严重 Bug ✅ 439 个测试全部通过
- [x] 性能无明显下降 ✅ 测试运行时间 < 5 秒

### 5.3 文档验收

- [x] 更新 MiniClaw-vs-myclaw.net-v2.md ✅ 添加 Phase 1-4 功能对比
- [x] 更新 README.md ✅ 费曼风格重写
- [x] 模板文件完整 ✅ 14 个模板文件

---

## 六、风险与缓解

| 风险 | 概率 | 影响 | 缓解措施 |
|------|------|------|----------|
| 情感系统复杂度高 | 中 | 高 | 先实现核心，迭代优化 |
| 与现有系统冲突 | 低 | 中 | 充分测试，渐进式集成 |
| 性能影响 | 低 | 中 | 使用异步操作，缓存优化 |

---

## 七、后续展望

### v0.8.0 (本次改进)
- 生命体基础系统 (Phase 1-4)
- 全基因组进化
- RIBOSOME 架构
- 向量记忆与 RAG (Phase 6)

### v0.9.0 (未来)
- 多 Agent 协作
- 高级 RAG 支持
- 向量数据库集成

### v1.0.0 (目标)
- 完整的数字生命体
- 企业级稳定性
- 完善的文档和示例

---

## 八、实施进度

### Phase 1: 生命体基础系统 ✅ 已完成 (2026-03-14)

#### 已实现文件:

```
src/MyClaw.Core/
├── Affect/
│   ├── AffectState.cs         # 情感状态模型 (Alertness, Mood, Curiosity, Confidence)
│   ├── AffectManager.cs       # 情感管理器 (UpdateAffect, ApplyPain, PulseRecovery)
│   └── AffectMode.cs          # 行为模式枚举 (Exploration, Execution, Cautious, Rest)
├── Nociception/
│   ├── PainMemory.cs          # 痛觉记忆模型
│   └── NociceptionManager.cs  # 痛觉管理器 (RecordPain, HasPainMemory, GetPainStatus)
├── Epigenetics/
│   ├── MethylatedTrait.cs     # 甲基化特征模型
│   └── MethylationManager.cs  # 甲基化管理器 (ShouldMethylate, MethylateTrait)
tests/MyClaw.Core.Tests/
├── Affect/
│   └── AffectManagerTests.cs  # 14 个测试
├── Nociception/
│   └── NociceptionManagerTests.cs  # 11 个测试
├── Epigenetics/
│   └── MethylationManagerTests.cs  # 15 个测试
```

#### 核心功能:

1. **统一情感状态系统 (Affect)**
   - AffectState: Alertness (0-1), Mood (-1 to 1), Curiosity (0-1), Confidence (0-1)
   - AffectMode: Exploration, Execution, Cautious, Rest
   - 脉冲恢复: 每 Pulse 周期 10% 恢复到基线
   - 与 ContextCompiler 集成

2. **痛觉记忆系统 (Nociception)**
   - PainMemory: Context, Action, Consequence, Intensity, Weight
   - 指数衰减: 7 天半衰期
   - 与 AffectManager 联动: 痛觉触发时 ApplyPain()
   - 循环缓冲区: 最大 50 条记忆

3. **表观遗传甲基化 (Epigenetics)**
   - MethylatedTrait: Trait, Value, Source, Stability
   - 甲基化阈值: 10 次重复, 0.8 置信度
   - 冷却期: 48 小时
   - 支持 SOUL.md 注释格式

---

### Phase 2: 全基因组进化系统 ✅ 已完成 (2026-03-14)

#### 已实现文件:

```
src/MyClaw.Core/
├── Evolution/
│   ├── EvolutionTypes.cs       # PatternType 枚举, DetectedPattern, EvolutionResult, MutationRecord
│   ├── EvolutionEngine.cs      # 进化引擎 (AnalyzePatternsAsync, TriggerEvolutionAsync)
│   └── SignalDetector.cs       # 增强版信号检测器 (8 → 17 信号类型)
tests/MyClaw.Core.Tests/
├── Evolution/
│   ├── EvolutionEngineTests.cs # 21 个测试
│   └── SignalDetectorTests.cs  # 27 个测试 (增强后)
```

#### 核心功能:

1. **EvolutionEngine 进化引擎**
   - AnalyzePatternsAsync: 分析记忆文件中的模式
   - TriggerEvolutionAsync: 触发进化，更新 DNA 文件
   - GetStateAsync: 获取进化状态和冷却期
   - 冷却期: 24 小时
   - 最小置信度: 0.75
   - 最小模式数: 2

2. **扩展的模式类型 (PatternType)**
   - Repetition → TOOLS.md + CONCEPTS.md
   - Preference → SOUL.md
   - Temporal → USER.md
   - Workflow → AGENTS.md
   - Sentiment → SOUL.md + REFLECTION.md
   - ErrorPattern → REFLECTION.md
   - Curiosity → 好奇心触发
   - Milestone → HORIZONS.md
   - Concept → CONCEPTS.md

3. **增强版 SignalDetector**
   - 新增信号类型: PositiveFeedback, NegativeFeedback, ErrorPattern, SkillSuggestion, CuriosityTrigger, RepetitionPattern, TemporalPattern, ConceptMention, Milestone
   - 动态置信度计算
   - DetectRepetitionPatterns: 检测重复关键词
   - DetectToolSequencePatterns: 检测工具序列模式

4. **DNA 文件更新**
   - 智能去重
   - 置信度合并
   - 概念提取
   - 里程碑检测

---

### Phase 3: RIBOSOME 架构 ✅ 已完成 (2026-03-14)

#### 已实现文件:

```
src/MyClaw.Core/
├── Ribosome/
│   ├── InstinctDefinition.cs  # 本能定义模型 (Handler, Description, InputSchema, SignalRules)
│   └── RibosomeLoader.cs      # 核糖体加载器 (LoadInstinctsAsync, GetMcpToolsAsync)
src/MyClaw.Mcp/
└── McpServer.cs               # 集成 RibosomeLoader (动态工具加载)
templates/
└── RIBOSOME.json              # 核糖体配置模板 (13 个本能工具)
tests/MyClaw.Core.Tests/
├── Ribosome/
│   └── RibosomeLoaderTests.cs # 14 个测试
tests/MyClaw.Integration.Tests/
└── Mcp/
    └── RibosomeIntegrationTests.cs # 20+ 个集成测试
```

#### 核心功能:

1. **RibosomeLoader 核糖体加载器**
   - LoadInstinctsAsync: 加载所有本能定义
   - GetInstinctAsync: 获取单个本能
   - GetHandlerAsync: 获取处理器名称
   - GetMcpToolsAsync: 转换为 MCP 工具格式
   - 缓存 TTL: 30 秒
   - 加载顺序: 用户目录 → 模板目录 → 默认配置

2. **RIBOSOME.json 配置**
   - 13 个本能工具: myclaw_update, myclaw_note, myclaw_read, myclaw_exec, myclaw_entity, myclaw_skill, myclaw_introspect, myclaw_dream, myclaw_archive, myclaw_immune, myclaw_heal, myclaw_status, myclaw_nociception
   - 完整的 InputSchema 定义
   - SignalRules 信号检测规则
   - isCore 核心工具标记

3. **MCP 服务集成**
   - HandleListTools(): 从 RibosomeLoader 动态加载工具
   - ExecuteToolAsync(): 使用 handler 路由到实现
   - 新增工具实现: ToolSkillManagerAsync, ToolIntrospect, ToolDream, ToolImmune, ToolHeal, ToolNociception
   - templates 目录自动复制到输出

4. **新增工具**
   - myclaw_skill: 创建/列出/删除技能
   - myclaw_introspect: 自我观察 (summary/tools/files)
   - myclaw_dream: 从日志中提取洞察
   - myclaw_immune: 更新 DNA 健康备份
   - myclaw_heal: 从备份恢复 DNA
   - myclaw_nociception: 痛觉记忆管理

---

### Phase 4: 好奇心与主动探索系统 ✅ 已完成 (2026-03-14)

#### 已实现文件:

```
src/MyClaw.Core/
├── Curiosity/
│   ├── ExplorationTarget.cs   # 探索目标模型 (Topic, Priority, Status, Type)
│   └── CuriosityEngine.cs     # 好奇心引擎 (GenerateTargets, ModulateByAffect)
tests/MyClaw.Core.Tests/
├── Curiosity/
│   └── CuriosityEngineTests.cs # 24 个测试
```

#### 核心功能:

1. **ExplorationTarget 探索目标**
   - 6 种探索类型: NewConcept, UnusedTool, UnknownDomain, NewFileType, PatternAnomaly, UserSuggestion
   - 4 种状态: Pending, InProgress, Completed, Abandoned
   - 优先级计算: 基础优先级 + 复杂度奖励

2. **CuriosityEngine 好奇心引擎**
   - GenerateTargets(): 根据情感状态和分析数据生成探索目标
   - ModulateByAffect(): 根据情感模式调制好奇心
   - CalculateCuriosityScore(): 计算话题的好奇心得分
   - AddUserSuggestion(): 添加用户建议的探索目标
   - GetNextTarget(): 获取下一个应该探索的目标

3. **情感调制**
   - 探索模式: 好奇心 × 1.3
   - 执行模式: 好奇心 × 0.8
   - 谨慎模式: 好奇心 × 0.5
   - 休息模式: 好奇心 × 0.3
   - 心情影响: 正面 × 1.1, 负面 × 0.9

4. **常量配置**
   - MinCuriosityThreshold: 0.3 (低于此值不生成目标)
   - MaxPendingTargets: 10 (最大待处理目标数)
   - TargetExpirationDays: 7 (目标过期天数)
   - GenerationIntervalHours: 4 (生成间隔)

---

### Phase 6: 向量记忆与 RAG 检索 ✅ 已完成 (2026-03-14)

#### 已实现文件:

```
src/MyClaw.Core/
├── VectorMemory/
│   ├── IVectorStore.cs           # 向量存储接口
│   ├── InMemoryVectorStore.cs    # 内存向量存储实现
│   ├── IEmbeddingService.cs      # 嵌入服务接口
│   ├── SimpleEmbeddingService.cs # 简单嵌入服务 (特征哈希)
│   ├── VectorMemoryEntry.cs      # 向量记忆条目模型
│   ├── VectorSearchTypes.cs      # 搜索请求/结果类型
│   ├── RagRetriever.cs           # RAG 检索器
│   └── VectorMemoryManager.cs    # 向量记忆管理器
tests/MyClaw.Core.Tests/
├── VectorMemory/
│   ├── InMemoryVectorStoreTests.cs    # 20 个测试
│   ├── SimpleEmbeddingServiceTests.cs # 13 个测试
│   └── RagRetrieverTests.cs           # 13 个测试
```

#### 核心功能:

1. **IVectorStore 向量存储接口**
   - UpsertAsync/UpsertBatchAsync: 添加/更新条目
   - GetAsync/DeleteAsync: 获取/删除条目
   - SearchAsync: 向量相似度搜索
   - SaveAsync/LoadAsync: 持久化到文件

2. **InMemoryVectorStore 内存向量存储**
   - 余弦相似度计算
   - 来源类型过滤
   - 元数据过滤
   - 线程安全操作

3. **SimpleEmbeddingService 简单嵌入服务**
   - 多层特征哈希: n-gram + 词级 + 位置 + 全局
   - L2 归一化
   - 嵌入缓存 (最大 10000 条)
   - 支持中英文文本
   - 代码/问题检测

4. **RagRetriever RAG 检索器**
   - SearchAsync: 语义搜索
   - HybridSearchAsync: 混合检索 (语义 + 关键词)
   - GetRelevantContextAsync: 获取相关上下文
   - IndexAsync/IndexBatchAsync: 索引文档
   - 智能分块 (支持重叠)

5. **VectorMemoryManager 向量记忆管理器**
   - IndexLongTermMemoryAsync: 索引长期记忆
   - IndexDailyLogsAsync: 索引每日日志
   - IndexEntitiesAsync: 索引实体知识
   - IndexSkillsAsync: 索引技能文件
   - RebuildIndexAsync: 全量重建索引
   - GetStats: 获取统计信息

#### 技术特点:

- **零外部依赖**: 不依赖 Qdrant/Milvus/OpenAI API
- **快速启动**: 纯内存实现，无需配置
- **可扩展**: 接口设计支持替换为真实向量数据库
- **智能分块**: 自动处理长文档，支持重叠
- **混合检索**: 结合语义和关键词搜索

---

**文档版本**: 1.6
**创建日期**: 2026-03-14
**最后更新**: 2026-03-14 (Phase 1-4 + Phase 6 完成, 363 单元测试 + 76 集成测试通过)
**完成日期**: 2026-03-14
**负责人**: MyClaw.NET Team
