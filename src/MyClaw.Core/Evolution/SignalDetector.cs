using System.Text.RegularExpressions;

namespace MyClaw.Core.Evolution;

/// <summary>
/// 进化信号类型
/// </summary>
public enum EvolutionSignal
{
    UserPreference,        // 用户偏好
    PersonalityCorrection, // 性格修正
    EnvironmentConfig,     // 环境配置
    ToolExperience,        // 工具经验
    IdentityChange,        // 身份改变
    WorkflowLearned,       // 工作流学习
    ImportantFact,         // 重要事实
    DailyLogEntry,         // 日常记录

    // Phase 2.2 新增信号类型
    PositiveFeedback,      // 正面反馈
    NegativeFeedback,      // 负面反馈
    ErrorPattern,          // 错误模式
    SkillSuggestion,       // 技能建议
    CuriosityTrigger,      // 好奇心触发
    RepetitionPattern,     // 重复模式
    TemporalPattern,       // 时间模式
    ConceptMention,        // 概念提及
    Milestone              // 里程碑
}

/// <summary>
/// 检测到的信号
/// </summary>
public class DetectedSignal
{
    /// <summary>
    /// 信号类型
    /// </summary>
    public EvolutionSignal SignalType { get; set; }

    /// <summary>
    /// 目标文件
    /// </summary>
    public string TargetFile { get; set; } = string.Empty;

    /// <summary>
    /// 建议工具
    /// </summary>
    public string SuggestedTool { get; set; } = string.Empty;

    /// <summary>
    /// 匹配内容
    /// </summary>
    public string MatchedContent { get; set; } = string.Empty;

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }
}

/// <summary>
/// 进化信号检测器 - 实现"信号 → 文件 → 工具"的自动化进化链
/// </summary>
public class SignalDetector
{
    // 信号检测模式 - Phase 2.2 增强版
    private readonly List<(EvolutionSignal Signal, string[] Patterns, string TargetFile, string Tool, double BaseConfidence)> _signalPatterns = new()
    {
        // 用户偏好 -> USER.md
        (EvolutionSignal.UserPreference,
            new[] { "我喜欢", "I like", "不要", "don't", "以后请", "please.*next time", "记住我喜欢", "remember I like", "偏好", "prefer" },
            "USER.md", "miniclaw_update", 0.8),

        // 性格修正 -> SOUL.md
        (EvolutionSignal.PersonalityCorrection,
            new[] { "别那么严肃", "less serious", "活泼一点", "more lively", "你是一个", "you are a", "改变性格", "change personality", "语气", "tone" },
            "SOUL.md", "miniclaw_update", 0.85),

        // 环境配置 -> TOOLS.md
        (EvolutionSignal.EnvironmentConfig,
            new[] { "项目用的是", "project uses", "服务器IP", "server IP", "路径是", "path is", "API key", "密钥", "环境变量", "environment" },
            "TOOLS.md", "miniclaw_update", 0.75),

        // 工具经验 -> TOOLS.md
        (EvolutionSignal.ToolExperience,
            new[] { "这个工具的参数", "tool parameter", "踩坑记录", "pitfall", "解决方案", "solution.*tool", "最佳实践", "best practice" },
            "TOOLS.md", "miniclaw_update", 0.7),

        // 身份改变 -> IDENTITY.md
        (EvolutionSignal.IdentityChange,
            new[] { "叫你自己", "call yourself", "记住你的名字是", "your name is", "改名", "rename", "你是谁", "who are you" },
            "IDENTITY.md", "miniclaw_update", 0.9),

        // 工作流学习 -> AGENTS.md
        (EvolutionSignal.WorkflowLearned,
            new[] { "最好的实践是", "best practice", "以后都按这个流程", "follow this workflow", "标准化", "standardize", "工作流", "workflow" },
            "AGENTS.md", "miniclaw_update", 0.8),

        // 重要事实 -> MEMORY.md
        (EvolutionSignal.ImportantFact,
            new[] { "重要", "important", "记住这个", "remember this", "别忘了", "don't forget", "mark this", "关键", "critical" },
            "MEMORY.md", "miniclaw_update", 0.85),

        // Phase 2.2 新增信号类型

        // 正面反馈 -> SOUL.md
        (EvolutionSignal.PositiveFeedback,
            new[] { "谢谢", "感谢", "很好", "不错", "perfect", "great", "太棒了", "awesome", "做得好", "well done" },
            "SOUL.md", "miniclaw_update", 0.7),

        // 负面反馈 -> REFLECTION.md
        (EvolutionSignal.NegativeFeedback,
            new[] { "不对", "错了", "糟糕", "wrong", "bad", "不好", "不满意", "disappointed", "问题", "issue" },
            "REFLECTION.md", "miniclaw_update", 0.75),

        // 错误模式 -> REFLECTION.md
        (EvolutionSignal.ErrorPattern,
            new[] { "error", "failed", "exception", "crash", "失败", "错误", "bug", "崩溃" },
            "REFLECTION.md", "miniclaw_update", 0.8),

        // 技能建议 -> TOOLS.md
        (EvolutionSignal.SkillSuggestion,
            new[] { "创建技能", "create skill", "自动完成", "automate", "应该有个工具", "should have tool", "常用操作", "common operation" },
            "TOOLS.md", "miniclaw_skill", 0.85),

        // 好奇心触发 -> HORIZONS.md
        (EvolutionSignal.CuriosityTrigger,
            new[] { "我想知道", "I wonder", "为什么", "why", "探索", "explore", "了解", "learn about", "研究", "research" },
            "HORIZONS.md", "miniclaw_update", 0.65),

        // 重复模式 -> TOOLS.md
        (EvolutionSignal.RepetitionPattern,
            new[] { "又遇到", "again", "重复", "repeat", "第三次", "third time", "每次都", "every time" },
            "TOOLS.md", "miniclaw_update", 0.75),

        // 概念提及 -> CONCEPTS.md
        (EvolutionSignal.ConceptMention,
            new[] { "概念", "concept", "术语", "terminology", "定义", "definition" },
            "CONCEPTS.md", "miniclaw_update", 0.6),

        // 里程碑 -> HORIZONS.md
        (EvolutionSignal.Milestone,
            new[] { "完成", "completed", "达成", "achieved", "里程碑", "milestone", "版本", "version" },
            "HORIZONS.md", "miniclaw_update", 0.8)
    };

    // 日常记录触发词 -> 每日日志
    private readonly string[] _dailyLogTriggers = {
        "记住这个", "mark", "note", "别忘了", "don't forget",
        "完成了", "finished", "下一步", "next step"
    };

    /// <summary>
    /// 检测用户输入中的进化信号
    /// </summary>
    public List<DetectedSignal> DetectSignals(string userInput)
    {
        var signals = new List<DetectedSignal>();
        if (string.IsNullOrWhiteSpace(userInput)) return signals;

        var lowerInput = userInput.ToLower();

        foreach (var (signal, patterns, targetFile, tool, baseConfidence) in _signalPatterns)
        {
            foreach (var pattern in patterns)
            {
                // 简单包含匹配
                if (lowerInput.Contains(pattern.ToLower()))
                {
                    signals.Add(new DetectedSignal
                    {
                        SignalType = signal,
                        TargetFile = targetFile,
                        SuggestedTool = tool,
                        MatchedContent = pattern,
                        Confidence = CalculateConfidence(baseConfidence, pattern, userInput)
                    });
                    break; // 该信号类型已匹配，不再检查其他模式
                }

                // 正则匹配（如果模式包含正则元字符）
                try
                {
                    if (Regex.IsMatch(userInput, pattern, RegexOptions.IgnoreCase))
                    {
                        signals.Add(new DetectedSignal
                        {
                            SignalType = signal,
                            TargetFile = targetFile,
                            SuggestedTool = tool,
                            MatchedContent = pattern,
                            Confidence = CalculateConfidence(baseConfidence + 0.1, pattern, userInput)
                        });
                        break;
                    }
                }
                catch { /* 忽略无效正则 */ }
            }
        }

        // 检测日常记录触发词
        foreach (var trigger in _dailyLogTriggers)
        {
            if (lowerInput.Contains(trigger.ToLower()))
            {
                // 检查是否已经被其他信号覆盖
                if (!signals.Any(s => s.SignalType == EvolutionSignal.ImportantFact))
                {
                    signals.Add(new DetectedSignal
                    {
                        SignalType = EvolutionSignal.DailyLogEntry,
                        TargetFile = $"memory/{DateTime.Now:yyyy-MM-dd}.md",
                        SuggestedTool = "miniclaw_note",
                        MatchedContent = trigger,
                        Confidence = 0.7
                    });
                }
                break;
            }
        }

        return signals.DistinctBy(s => s.SignalType).ToList();
    }

    /// <summary>
    /// 计算信号置信度
    /// </summary>
    private static double CalculateConfidence(double baseConfidence, string matchedPattern, string fullInput)
    {
        var confidence = baseConfidence;

        // 如果匹配的模式较长，增加置信度
        if (matchedPattern.Length > 5)
            confidence += 0.05;

        // 如果输入中包含多个相关关键词，增加置信度
        var contextBoosters = new[] { "一定要", "must", "非常重要", "very important", "请记住", "please remember" };
        foreach (var booster in contextBoosters)
        {
            if (fullInput.ToLower().Contains(booster.ToLower()))
            {
                confidence += 0.1;
                break;
            }
        }

        // 限制最大置信度
        return Math.Min(confidence, 0.98);
    }

    /// <summary>
    /// 从对话历史中检测重复模式
    /// </summary>
    public List<DetectedSignal> DetectRepetitionPatterns(List<string> recentInputs)
    {
        var signals = new List<DetectedSignal>();
        if (recentInputs.Count < 3) return signals;

        // 检测重复关键词
        var wordCounts = new Dictionary<string, int>();
        foreach (var input in recentInputs)
        {
            var words = input.ToLower().Split(new[] { ' ', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(w => w.Length > 3);
            foreach (var word in words)
            {
                wordCounts[word] = wordCounts.GetValueOrDefault(word, 0) + 1;
            }
        }

        var repeatedWords = wordCounts.Where(kvp => kvp.Value >= 3).ToList();
        if (repeatedWords.Count > 0)
        {
            signals.Add(new DetectedSignal
            {
                SignalType = EvolutionSignal.RepetitionPattern,
                TargetFile = "TOOLS.md",
                SuggestedTool = "miniclaw_skill",
                MatchedContent = string.Join(", ", repeatedWords.Take(5).Select(w => w.Key)),
                Confidence = 0.75
            });
        }

        return signals;
    }

    /// <summary>
    /// 检测工具使用序列模式
    /// </summary>
    public List<DetectedSignal> DetectToolSequencePatterns(string content)
    {
        var signals = new List<DetectedSignal>();
        var toolMatches = Regex.Matches(content, @"miniclaw_[a-z_]+");

        if (toolMatches.Count < 6) return signals;

        var toolSequence = toolMatches.Cast<Match>().Select(m => m.Value).ToList();

        // 检测 2-3 步重复序列
        for (var len = 2; len <= 3; len++)
        {
            var sequences = new Dictionary<string, int>();
            for (var i = 0; i <= toolSequence.Count - len; i++)
            {
                var seq = string.Join(" → ", toolSequence.Skip(i).Take(len));
                sequences[seq] = sequences.GetValueOrDefault(seq, 0) + 1;
            }

            var repeated = sequences.Where(kvp => kvp.Value >= 2).ToList();
            if (repeated.Count > 0)
            {
                var top = repeated.OrderByDescending(kvp => kvp.Value).First();
                signals.Add(new DetectedSignal
                {
                    SignalType = EvolutionSignal.WorkflowLearned,
                    TargetFile = "AGENTS.md",
                    SuggestedTool = "miniclaw_update",
                    MatchedContent = $"Repeated {len}-step workflow: {top.Key}",
                    Confidence = 0.8
                });
                break;
            }
        }

        return signals;
    }

    /// <summary>
    /// 生成进化建议
    /// </summary>
    public string GenerateEvolutionAdvice(List<DetectedSignal> signals)
    {
        if (signals.Count == 0) return string.Empty;

        var lines = new List<string>();
        lines.Add("🧬 检测到进化信号:");

        foreach (var signal in signals)
        {
            lines.Add($"  • {signal.SignalType} → {signal.TargetFile} (使用 {signal.SuggestedTool})");
        }

        lines.Add("\n建议执行相应的工具调用以更新记忆。");

        return string.Join("\n", lines);
    }
}
