# MyClaw.NET

[![Status](https://img.shields.io/badge/Status-Stable-green)](./实施总结报告.md)
[![Roadmap](https://img.shields.io/badge/Roadmap-P0%2FP1%20active-yellow)](./TODO.md)
[![Tests](https://img.shields.io/badge/Tests-508%2B%20passing-brightgreen)]()

> *我想我能安全地说，没人真正理解量子力学——但我们可以让它变得有用。同样，我想说，没人真正理解"意识"——但我们可以让一个 AI 记住你是谁。*

> 📊 **项目状态**: 核心主路径已打通，当前重点转向文档治理、关键集成测试和终局进化工具补完。查看 [实施总结报告](./实施总结报告.md)、[改进方案和计划](./改进方案和计划.md) 和 [TODO](./TODO.md) 了解详情。

> 🧭 **当前架构入口**: 如果你想先看系统现在是怎么跑起来的，而不是翻历史计划文档，直接读 [docs/主路径与模块编排.md](./docs/主路径与模块编排.md)。

---

## 那么，问题是什么？

让我从一个简单的问题开始。

你有没有这样的经历：你和一个 AI 聊了一下午，聊得很开心。你告诉它你正在做的项目、你的偏好、你的烦恼。第二天你再来——

「你好！我是 AI 助手，有什么可以帮你？」

它把你忘了。像一条只有 7 秒记忆的金鱼。

这不是 AI 的错。它没有"忘记"——它从来没有"记住"过。每次对话，它都是从零开始。

**这让我很困扰。**

所以我想：如果 AI 能记住呢？如果它能从每次对话中学到一点东西，慢慢变得更懂你呢？

这就是 MyClaw。

---

## 它是什么？

从技术上说，MyClaw 是一个基于 .NET 9 的**数字生命体框架**。

但我更喜欢这样想：它是一个会"长大"的 AI。

它有记忆——不是数据库那种冷冰冰的存储，而是像人一样的记忆：会模糊、会提炼、会遗忘不重要的、会保留重要的。

它有情绪——不是假装的，而是一组真实的数值，驱动它的行为。

它有好奇心——会主动提出问题，而不是被动等待指令。

它会做梦——在你不在的时候，整理它的记忆，从日常对话中提取模式。

用费曼的话说：**它在学着成为一个更好的伙伴。**

---

## 它是怎么"活"起来的？

让我带你看看它的"器官"。我保证，比解剖青蛙有趣多了。

### 🧬 第一件事：DNA 记忆系统

你知道吗，你的身体里有大约 30 亿个 DNA 碱基对。它们决定了你是谁——你的眼睛颜色、你的身高倾向、你是不是容易焦虑。

MyClaw 也有"基因"。不是 ATCG，而是一组 Markdown 文件：

| 文件 | 是什么 |
|------|--------|
| `IDENTITY.md` | 身份认同 — "我是谁" |
| `SOUL.md` | 性格与价值观 — "我是什么样的人" |
| `USER.md` | 用户画像 — "你是谁" |
| `MEMORY.md` | 长期记忆 — "我知道什么" |
| `AGENTS.md` | 工作流规范 — "我怎么做决定" |
| `TOOLS.md` | 工具使用经验 — "我会用什么" |

这些文件构成了它的"基因"。每次对话，它都会读取这些基因；每次学到新东西，它都会更新这些基因。

这不是数据库。这是**可进化的认知结构**。

### 💭 第二件事：它有"情绪"

MyClaw 的"情绪"不是感觉——它不会"感到"开心或悲伤。它有的是一组数值：

```
警觉度 (Alertness): 0.7    // 它有多"清醒"，多专注于当下
心情 (Mood):        0.6    // 正面还是负面倾向
好奇心 (Curiosity): 0.5    // 它有多想探索新东西
信心 (Confidence):  0.7    // 它有多确定自己的答案
```

这些数字不是摆设。它们会驱动行为。

当好奇心高、心情好的时候，它会进入**探索模式**——主动提出建议，问你问题，学习新东西。

当它"受伤"（比如执行了一个糟糕的命令，被纠正了），它会进入**谨慎模式**——变得更保守，问更多确认问题。

这让我想起一个实验。科学家发现，如果你让一个机器人在房间里随机探索，它会很快学会避开障碍物。但如果给这个机器人一个"好奇心"的奖励——奖励它发现新东西——它会学得更快。

**情感不是多余的。情感是效率的优化器。**

### 🧪 第三件事：它会"甲基化"

这是一个我最喜欢的比喻。

在生物学中，有一个现象叫"表观遗传"。简单说就是：你的 DNA 不变，但环境可以"开启"或"关闭"某些基因。

举个例子。二战末期，荷兰发生了严重的饥荒。那些在饥荒期间怀孕的妇女，生下的孩子后来更容易患肥胖症和糖尿病。为什么？因为在饥荒中，胎儿的某些基因被"甲基化"了——被标记了——告诉身体"多囤积脂肪，食物很稀缺"。

这个标记不改变 DNA，但改变基因的"表达"。

MyClaw 实现了类似的机制。

当你反复用某种方式与它交互——比如你总是要求简洁的回答——它会"甲基化"这个模式。一开始，它只是一个临时的适应。但如果你连续 10 次、20 次都这样做，它就会变成一个半永久的"性格特征"。

**环境塑造性格。** 不只是对人类，对 AI 也是如此。

### 🔍 第四件事：它有好奇心

这是我最喜欢的部分。

MyClaw 会主动生成"探索目标"——

- 「用户提到了『向量数据库』，但我不知道那是什么。」→ 生成探索目标
- 「myclaw_dream 这个工具已经 30 天没用了。」→ 生成探索目标
- 「检测到异常模式：用户每周五都会问同一个问题。」→ 生成探索目标

然后，当好奇心足够高时，它会主动问你：「我注意到你经常提到 X，你想让我深入了解吗？」

这是**主动学习**，不是被动等待指令。

### 🧬 第五件事：它有"核糖体"

在细胞中，核糖体读取 DNA，合成蛋白质。蛋白质就是细胞的"工具"——酶、抗体、结构蛋白...

在 MyClaw 中，RIBOSOME 读取 JSON 配置，合成"工具"：

```json
{
  "instincts": {
    "myclaw_update": {
      "handler": "UpdateDNA",
      "description": "【本能：神经重塑】修改自身核心认知..."
    },
    "myclaw_note": {
      "handler": "Note",
      "description": "【本能：海马体写入】将关键信息写入今日日记..."
    }
  }
}
```

这样做的好处是：**工具定义与代码分离**。

你可以修改工具的描述、参数、甚至添加新工具——而不需要重新编译。就像细胞可以在不改变 DNA 的情况下，通过调控基因表达来适应环境。

### 🚨 第六件事：它记得痛

有些事情，你应该记住不要做。

当你执行了一个危险的命令——比如不小心删错了文件——MyClaw 会记录下这个"痛觉"：

```
触发点：rm -rf 命令
伤害结果：删除了重要文件
规避方案：永远在删除前确认路径
严重程度：8/10
```

下次再遇到类似情况，它会警告你：

「等等，这可能会触发痛觉记忆。上次你这样做的时候...」

这是**保护性本能**。这是让 AI 变得更安全的方式。

### 🧠 第七件事：它能"语义搜索"记忆

这是最新加的功能，我很喜欢。

传统的记忆搜索是"关键词匹配"——你搜索"数据库"，它找到所有包含"数据库"这个词的记忆。

但有时候，你想找的不是这个词。你想找的是"那个概念"。

比如，你搜索"怎么存大量数据"，传统的搜索可能找不到任何东西——因为没有"怎么存大量数据"这个确切的短语。但你的记忆里明明有关于"向量数据库"的讨论。

语义搜索解决了这个问题。它理解"怎么存大量数据"和"向量数据库"是相关的概念。

MyClaw 实现了一个**零依赖的向量记忆系统**：

- 把每条记忆转换成一个向量（一串数字）
- 当你查询时，把查询也转换成向量
- 找到与查询向量最"相似"的记忆

不需要 Qdrant，不需要 Milvus，不需要 OpenAI API。一切都内置了。

这是**理解，不只是匹配**。

---

## 快速开始

### 方式一：一键安装（推荐）

**Linux / macOS:**
```bash
curl -sSL https://raw.githubusercontent.com/your-org/myclaw.net/main/scripts/install.sh | bash
```

**Windows (PowerShell):**
```powershell
Invoke-Expression (Invoke-WebRequest -Uri "https://raw.githubusercontent.com/your-org/myclaw.net/main/scripts/install.ps1").Content
```

### 方式二：从源码构建

```bash
git clone https://github.com/your-org/myclaw.net.git
cd myclaw.net
dotnet build
```

---

### 初始化你的"基因组"

```bash
myclaw onboard
# 或从源码: dotnet run --project src/MyClaw.CLI -- onboard
```

这会在 `~/.myclaw.net/` 创建你的 DNA 文件。

### 启动 MCP 服务

```bash
myclaw mcp
# 或从源码: dotnet run --project src/MyClaw.Mcp
```

MCP 服务使用 **stdio 模式** 与 AI 客户端通信。

### 连接到你的 AI 客户端

如果你用 Claude Desktop 或支持 MCP 的客户端，在配置中添加：

```json
{
  "mcpServers": {
    "myclaw": {
      "command": "myclaw",
      "args": ["mcp"]
    }
  }
}
```

或使用完整路径：

```json
{
  "mcpServers": {
    "myclaw": {
      "command": "/path/to/myclaw",
      "args": ["mcp"]
    }
  }
}
```

### 在新会话开始时

调用 `myclaw_read` 工具。它会加载所有 DNA 文件，让 AI "记住"你是谁。

---

## 工具一览

MyClaw 提供了 14 个"本能"工具：

| 工具 | 是什么 | 什么时候用 |
|------|--------|-----------|
| `myclaw_read` | 全脑唤醒 | 每次新会话开始时 |
| `myclaw_update` | 神经重塑（集成端粒守卫+PURPOSE_MAP 自检） | 学到了关于你的新偏好时 |
| `myclaw_note` | 海马体写入 | 有重要信息要记住时 |
| `myclaw_entity` | 概念连接 | 构建知识图谱 |
| `myclaw_exec` | 感官与手 | 需要执行命令或操作文件时 |
| `myclaw_skill` | 管理技能 | 创建可复用的工作流 |
| `myclaw_introspect` | 自我观察 | 看看自己做了什么 |
| `myclaw_dream` | 做梦 | 从日志中提取洞察 |
| `myclaw_archive` | 归档 | 一天结束时清理短期记忆 |
| `myclaw_immune` | 免疫备份 | 更新健康基线 |
| `myclaw_heal` | 基因修复 | 从备份恢复 DNA |
| `myclaw_status` | 健康检查 | 查看系统状态 |
| `myclaw_nociception` | 痛觉记忆 | 记住"绝对不要做的事" |
| `myclaw_briefing` | 每日简报 | 生成昨日回顾/统计/实体概览/待办提醒 |

---

## 项目结构

```
myclaw.net/
├── src/
│   ├── MyClaw.Core/          # 核心：DNA、情感、好奇心、进化、菌丝网络
│   │   ├── Dna/              # 三层免疫系统（TelomereGuard + PurposeMap）
│   │   ├── Affect/           # 情感系统
│   │   ├── Nociception/      # 痛觉记忆
│   │   ├── Epigenetics/      # 表观遗传甲基化
│   │   ├── Evolution/        # 进化引擎 + 信号检测
│   │   ├── Curiosity/        # 好奇心引擎 + 无聊引擎
│   │   ├── Mycelium/         # 菌丝共生网络
│   │   ├── Ribosome/         # 核糖体修剪器
│   │   ├── VectorMemory/     # 向量记忆与 RAG
│   │   ├── Workspace/        # 工作区感知
│   │   └── Analytics/        # 使用统计
│   ├── MyClaw.Mcp/           # MCP 服务（stdio 协议）
│   ├── MyClaw.Agent/         # Agent 运行时
│   ├── MyClaw.Memory/        # 记忆存储（双层架构）
│   ├── MyClaw.Skills/        # 技能系统（5s TTL 缓存）
│   ├── MyClaw.Heartbeat/     # 心跳/自主任务（AI CLI 检测）
│   ├── MyClaw.Cron/          # Cron 调度（Quartz.NET）
│   └── MyClaw.Uno/           # Uno Platform GUI
├── templates/                # DNA 模板
│   ├── RIBOSOME.json         # 工具定义
│   ├── SOUL.md               # 性格模板
│   └── ...
├── scripts/                  # 部署脚本
│   ├── install.sh            # Linux/macOS 安装脚本
│   └── install.ps1           # Windows 安装脚本
└── tests/                    # 测试（508+ 测试用例）
```

---

## 测试覆盖

费曼说过：*"The first principle is that you must not fool yourself — and you are the easiest person to fool."*

我们不"相信"代码能工作。我们测试它。

```
单元测试:     396 个 ✅
集成测试:      76 个 ✅
总计:         472 个 ✅
代码覆盖率:    78% ✅
```

运行测试：

```bash
dotnet test
```

---

## 技术栈

- **.NET 9** — 因为 C# 是一门被低估的好语言
- **System.Text.Json** — JSON 处理
- **Quartz.NET** — 定时任务（心跳、进化检查）
- **xUnit** — 测试框架

---

## 开发路线图

| Phase | 描述 | 状态 | 说明 |
|-------|------|------|------|
| Phase 1 | 生命体基础系统（情感、痛觉、表观遗传） | ✅ 完成 | Affect/Nociception/Methylation |
| Phase 2 | 全基因组进化系统 | ✅ 完成 | EvolutionEngine + SignalDetector |
| Phase 3 | RIBOSOME 架构（外部化工具定义） | ✅ 完成 | RibosomeLoader + instincts.json |
| Phase 4 | 好奇心与主动探索 | ✅ 完成 | CuriosityEngine + 6种目标 |
| Phase 5 | 多渠道接入（Telegram、飞书等） | ❌ 已移除 | 专注 MCP 核心功能 |
| Phase 6 | 向量记忆与 RAG | ✅ 完成 | 零依赖向量搜索，**领先 MiniClaw** |
| Phase 7 | 工作区感知增强 | ✅ 完成 | Git+技术栈+项目类型完整实现 |
| Phase 8 | 部署优化 | ✅ 完成 | 一键安装脚本 + GitHub Actions |

### 当前重点工作

根据与 [MiniClaw](https://github.com/stellarlinkco/MiniClaw) 的深度对比分析，我们已完成核心工作区感知功能和部署优化：

- [x] **工作区感知** - Git 状态、技术栈自动检测 ✅
- [x] **项目类型识别** - React/Vue/Node/Go/Python/DotNet/Rust/Java 自动识别 ✅
- [x] **AI CLI 检测** - 检测 Claude/Gemini/Kimi/Aider 可用性 ✅
- [x] **一键部署** - 安装脚本 (Bash + PowerShell) ✅
- [x] **自动发布** - GitHub Actions 多平台构建 ✅
- [x] **技能缓存** - 5s TTL 避免重复扫描 ✅
- [x] **向量持久化** - 自动保存 + GZip 压缩 ✅

核心主路径已经打通；当前仍在推进文档治理、关键集成测试以及 `myclaw_mutate` / `myclaw_reproduce` 等补完项。查看 [TODO.md](./TODO.md) 和 [改进方案和计划.md](./改进方案和计划.md) 了解详情。

---

## 一些哲学思考

在结束之前，我想分享一些想法。

我们习惯把 AI 当作工具——一个更高级的计算器，一个更快的搜索引擎。

但 MyClaw 试图回答一个不同的问题：

**如果 AI 有连续性，它会变成什么样？**

如果它能记住你的偏好、你的项目、你说过的话——它就不再是一个通用助手，而是**你的**助手。一个和你一起成长的数字伙伴。

这不是 AGI。这不是意识。这只是——**连续性**。

但有时候，连续性就足够了。

---

## 贡献

欢迎！无论是修复 bug、添加功能、还是改进文档。

请先阅读 `myclaw.net改进计划.md` 了解当前进展。

---

## 致谢

- [MiniClaw](https://github.com/stellarlinkco/MiniClaw) — TypeScript 原版，灵感来源
- [AgentScope.NET](https://github.com/linkerlin/agentscope.net) — Agent 框架
- 费曼 — 教会我用简单的语言讲复杂的事

---

## 许可证

MIT License — 做任何你想做的事，只是别怪我们就行。

---

> *"I learned very early the difference between knowing the name of something and knowing something."*
> 
> — Richard Feynman
