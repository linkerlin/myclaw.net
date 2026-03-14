namespace MyClaw.Core.Evolution;

/// <summary>
/// 模式类型 - 从记忆中检测到的行为模式
/// Pattern types detected from memory
/// </summary>
public enum PatternType
{
    /// <summary>
    /// 重复问题 - 建议创建技能
    /// Repetition patterns - suggest creating skills
    /// </summary>
    Repetition,

    /// <summary>
    /// 用户偏好 - 写入 USER.md / SOUL.md
    /// User preferences - write to USER.md / SOUL.md
    /// </summary>
    Preference,

    /// <summary>
    /// 时间模式 - 写入 USER.md
    /// Temporal patterns - write to USER.md
    /// </summary>
    Temporal,

    /// <summary>
    /// 工作流模式 - 写入 AGENTS.md
    /// Workflow patterns - write to AGENTS.md
    /// </summary>
    Workflow,

    /// <summary>
    /// 情感反馈 - 写入 SOUL.md
    /// Sentiment feedback - write to SOUL.md
    /// </summary>
    Sentiment,

    /// <summary>
    /// 错误模式 - 写入 REFLECTION.md + NOCICEPTION.md
    /// Error patterns - write to REFLECTION.md + NOCICEPTION.md
    /// </summary>
    ErrorPattern,

    /// <summary>
    /// 好奇心触发 - 主动探索
    /// Curiosity trigger - active exploration
    /// </summary>
    Curiosity,

    /// <summary>
    /// 里程碑 - 写入 HORIZONS.md
    /// Milestone - write to HORIZONS.md
    /// </summary>
    Milestone,

    /// <summary>
    /// 概念提取 - 写入 CONCEPTS.md
    /// Concept extraction - write to CONCEPTS.md
    /// </summary>
    Concept
}

/// <summary>
/// 检测到的模式
/// Detected pattern from memory analysis
/// </summary>
public class DetectedPattern
{
    /// <summary>
    /// 模式类型
    /// </summary>
    public PatternType Type { get; set; }

    /// <summary>
    /// 置信度 (0-1)
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 模式描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 建议操作
    /// </summary>
    public string? Suggestion { get; set; }

    /// <summary>
    /// 合并计数 (来自多少个相似模式)
    /// </summary>
    public int MergedCount { get; set; } = 1;

    /// <summary>
    /// 平均置信度
    /// </summary>
    public double AvgConfidence { get; set; }

    /// <summary>
    /// 目标文件
    /// </summary>
    public string TargetFile { get; set; } = string.Empty;
}

/// <summary>
/// 进化结果
/// </summary>
public class EvolutionResult
{
    /// <summary>
    /// 是否发生了进化
    /// </summary>
    public bool Evolved { get; set; }

    /// <summary>
    /// 结果消息
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 应用的模式列表
    /// </summary>
    public List<DetectedPattern> AppliedPatterns { get; set; } = new();

    /// <summary>
    /// 总进化次数
    /// </summary>
    public int TotalEvolutions { get; set; }
}

/// <summary>
/// 进化突变记录
/// </summary>
public class MutationRecord
{
    /// <summary>
    /// 目标文件
    /// </summary>
    public string Target { get; set; } = string.Empty;

    /// <summary>
    /// 变更描述
    /// </summary>
    public string Change { get; set; } = string.Empty;

    /// <summary>
    /// 置信度
    /// </summary>
    public double Confidence { get; set; }
}
