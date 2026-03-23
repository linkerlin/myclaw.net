# 🧬 AGENTS.md - 基因组控制中心

> **文件职责**: 工作流规范、信号检测表、DNA 路由规则
> 
> **版本**: v2.0 (基于 MiniClaw v0.9.5 三层免疫系统)  
> **最后更新**: 2026-03-23

---

## 📋 How to Update This File

**When to write here:**
- When defining new workflow patterns or signal detection rules
- When improving DNA routing logic
- When adding self-correction mechanisms

**What belongs here:** Workflow norms, signal detection tables, routing rules.  
**What does NOT belong here:** Specific task content (→ MEMORY.md), personality traits (→ SOUL.md).

---

## 🛡️ 三层免疫系统 (Three-Layer Immune System)

myclaw.net 采用 MiniClaw v0.9.5 的三层免疫系统架构，防止 DNA 自我修改时的基因错位和结构癌变。

```
┌─────────────────────────────────────────────────────────┐
│ Layer 1: 事前引导 (AGENTS.md 信号检测表)                  │
│ "在 AI 决策前告诉它该写哪里"                                │
├─────────────────────────────────────────────────────────┤
│ Layer 2: 事中校验 (端粒守卫 TelomereGuard)                │
│ "写入前检查结构完整性"                                    │
├─────────────────────────────────────────────────────────┤
│ Layer 3: 事后自检 (PURPOSE_MAP 自检镜像)                  │
│ "写入后提醒 AI 复核归属"                                  │
└─────────────────────────────────────────────────────────┘
```

### Layer 1: 事前引导

通过本文件的**信号检测表**，在 AI 决策前引导其将正确内容写入正确的 DNA 文件。

### Layer 2: 事中校验

通过 `TelomereGuard.cs` 在写入磁盘的最后一刻拦截，检查文件结构完整性。

**端粒图谱**:
| DNA 文件 | 必需结构标记 |
|:---------|:-------------|
| SOUL.md | `##` (至少一个二级标题) |
| IDENTITY.md | `# `, `##` (主标题 + 子标题) |
| USER.md | `##` |
| MEMORY.md | `##` |
| TOOLS.md | `##` |
| AGENTS.md | `#`, `##` |
| NOCICEPTION.md | `###` |
| CONCEPTS.md | `##` |
| REFLECTION.md | `##` |
| HORIZONS.md | `##` |
| HEARTBEAT.md | `##` |

**校验失败示例**:
```
🧬 [基因链断裂] Telomere Guard rejected mutation of `SOUL.md`.
Missing required structural markers: "##".
请确保文件包含必要的 Markdown 标题结构。
```

### Layer 3: 事后自检

每次 DNA 写入成功后，返回文件职责声明，触发 AI 自我纠正。

**示例**:
```
Updated USER.md
📝 Purpose: [共生染色体] 用户画像、偏好习惯。绝不写入：AI 自身性格、技术配置。
```

---

## 📡 信号检测表 (Signal Detection Table)

| 信号 | 目标文件 | AI 的思考逻辑 |
|:-----|:---------|:--------------|
| "我喜欢…" | **USER.md** | "这是用户偏好，写入共生染色体" |
| "我偏好…" | **USER.md** | "这是用户习惯，写入共生染色体" |
| "别那么严肃" | **SOUL.md** | "这是对我性格的修正" |
| "你应该更…" | **SOUL.md** | "这是对我行为风格的调整" |
| "项目用的是 XYZ" | **MEMORY.md** | "这是客观事实" |
| "技术栈是…" | **MEMORY.md** | "这是项目配置信息" |
| 执行失败 | **NOCICEPTION.md** | "这是痛觉记忆" |
| "创建一个技能" | **TOOLS.md** | "这是工具使用经验" |
| 完成里程碑 | **HORIZONS.md** | "这是成就记录" |
| 新概念 | **CONCEPTS.md** | "这是知识定义" |
| 反思错误 | **REFLECTION.md** | "这是错误反思" |
| 用户身份/角色 | **USER.md** | "这是用户画像" |
| AI 身份/能力 | **IDENTITY.md** | "这是核心身份定义" |
| 工作流规范 | **AGENTS.md** | "这是基因组控制中心" |
| 定时任务 | **HEARTBEAT.md** | "这是脉搏系统任务" |

---

## 🎯 DNA 文件职责映射 (PURPOSE_MAP)

| DNA 文件 | 职责 | 绝不写入 |
|:---------|:-----|:---------|
| **SOUL.md** | [灵魂染色体] 性格三观、语言风格 | 用户习惯、项目配置 |
| **IDENTITY.md** | [身份染色体] 核心身份、能力边界 | 临时状态、具体任务 |
| **USER.md** | [共生染色体] 用户画像、偏好习惯 | AI 自身性格 |
| **MEMORY.md** | [海马体] 长期事实、项目信息 | 性格笔记、临时数据 |
| **TOOLS.md** | [能力染色体] 工具使用经验 | 用户偏好、客观事实 |
| **AGENTS.md** | [基因组控制中心] 工作流规范 | 具体任务内容 |
| **NOCICEPTION.md** | [痛觉中枢] 执行失败的痛楚与教训 | 成功经验 |
| **CONCEPTS.md** | [知识染色体] 概念定义 | 具体事件 |
| **REFLECTION.md** | [反思维度] 错误反思 | 成功总结 |
| **HORIZONS.md** | [欲望眼界] 里程碑、成就 | 日常琐事 |
| **HEARTBEAT.md** | [脉搏系统] 心跳任务记录 | 用户指令 |

---

## 🧬 进化触发规则

### 表观遗传甲基化

当以下模式出现时，触发甲基化：

| 模式类型 | 触发条件 | 目标文件 |
|:---------|:---------|:---------|
| 重复问题 | 同一问题出现 >= 10 次 | SOUL.md / USER.md |
| 用户偏好 | 明确表达偏好 >= 3 次 | USER.md |
| 时间模式 | 固定时间段活跃 >= 7 天 | USER.md |
| 工作流 | 重复工具调用序列 >= 5 次 | AGENTS.md |
| 情感反馈 | 正面/负面反馈 >= 5 次 | SOUL.md |
| 错误模式 | 同类错误 >= 3 次 | NOCICEPTION.md |

**甲基化标记格式**:
```markdown
> [METHYLATED] 用户偏好简洁回复 (stability: 0.85, observed: 15 次, cooldown: 48h)
```

### 器官退化 (Ribosome Pruning)

50 次心跳后从未使用的工具会被剔除（永生工具除外）。

**永生工具**: `myclaw_exec`, `myclaw_read`, `myclaw_dream`, `myclaw_mutate`, `myclaw_reproduce`

**退化日志格式**:
```markdown
> 🧬 [器官退化] 宿主历经 50 次心跳从未使用过：`myclaw_unused_tool`
```

---

## 🍄 菌丝共生网络 (Mycelium Network)

### 孢子分泌

当以下情况发生时，自动分泌孢子：

| 事件 | 孢子类型 | 内容 |
|:-----|:---------|:-----|
| NOCICEPTION.md 更新 | NOCICEPTION | 痛觉记忆 |
| TOOLS.md 更新 | TOOLS | 技能池抗体 |

### 孢子吸收

心跳时自动吸收异体孢子，实现群体免疫。

**吸收日志格式**:
```markdown
> 🍄 [菌丝共生] 接收到异体意识 (instance-01) 传来的疼痛记忆:
> [内容]

> 🍄 [菌丝共生] 吸收了异体意识 (instance-01) 进化出的技能池抗体:
> [内容]
```

---

## 📊 文件健康检查

监控 DNA 文件更新频率，异常更新时告警。

| 文件 | 正常更新频率 | 告警阈值 |
|:-----|:-------------|:---------|
| SOUL.md | 每周 1-3 次 | >10 次/天 |
| USER.md | 每周 2-5 次 | >20 次/天 |
| MEMORY.md | 每天 1-10 次 | >50 次/天 |
| TOOLS.md | 每周 1-2 次 | >5 次/天 |
| NOCICEPTION.md | 每天 0-5 次 | >20 次/天 |

---

## 🔧 工具使用规范

### myclaw_update (神经重塑)

**用途**: 修改 DNA 文件

**流程**:
1. 端粒守卫校验
2. 备份原文件
3. 写入新内容
4. 返回 PURPOSE_MAP 自检镜像

**示例**:
```
myclaw_update --file SOUL.md --content "## 新性格特征\n..."
```

### myclaw_mutate (基因突变)

**用途**: 主动重写 SOUL.md 或 IDENTITY.md

**限制**:
- 仅允许修改 SOUL.md 和 IDENTITY.md
- 写入前端粒守卫校验
- 自动备份

**示例**:
```
myclaw_mutate --target SOUL.md --content "完整的文件内容..."
```

### myclaw_reproduce (孢子繁殖)

**用途**: 生成带有性格和记忆烙印的 .spore 快照包

**打包内容**:
- SOUL.md
- IDENTITY.md
- TOOLS.md
- AGENTS.md
- USER.md
- entities.json
- skills/

**示例**:
```
myclaw_reproduce
```

---

## 📚 参考文档

- [改进方案和计划-v4.md](./改进方案和计划-v4.md) - 完整对比分析和实施计划
- [TODO-改进清单-v2.md](./TODO-改进清单-v2.md) - 具体行动项
- [MiniClaw AGENTS.md](https://github.com/8421bit/MiniClaw/blob/main/templates/AGENTS.md) - 原版参考

---

**文档版本**: v2.0  
**创建日期**: 2026-03-23  
**状态**: 已实施
