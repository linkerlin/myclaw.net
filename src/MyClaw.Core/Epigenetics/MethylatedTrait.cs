namespace MyClaw.Core.Epigenetics;

/// <summary>
/// 甲基化特征 - 半永久性行为适应
/// Methylated Trait - semi-permanent behavioral adaptation
/// </summary>
public class MethylatedTrait
{
    /// <summary>
    /// 特征名称 (如 interaction_style, activity_pattern, workflow_style)
    /// Trait name
    /// </summary>
    public string Trait { get; set; } = string.Empty;

    /// <summary>
    /// 特征值 (如 proactive_modifier, time_sensitive, structured)
    /// Trait value
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// 来源描述 (原始模式描述)
    /// Source pattern description
    /// </summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// 甲基化时间
    /// Timestamp when methylated
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 模式重复次数
    /// Pattern repetition count
    /// </summary>
    public int PatternCount { get; set; }

    /// <summary>
    /// 稳定性 (0-1) - 越高越稳定
    /// Stability level (0-1)
    /// </summary>
    public double Stability { get; set; }

    /// <summary>
    /// 创建副本
    /// </summary>
    public MethylatedTrait Clone()
    {
        return new MethylatedTrait
        {
            Trait = Trait,
            Value = Value,
            Source = Source,
            Timestamp = Timestamp,
            PatternCount = PatternCount,
            Stability = Stability
        };
    }
}

/// <summary>
/// 特征类型
/// </summary>
public enum TraitType
{
    /// <summary>
    /// 交互风格 (如 proactive_modifier, active_reader)
    /// </summary>
    InteractionStyle,

    /// <summary>
    /// 活动模式 (如 time_sensitive)
    /// </summary>
    ActivityPattern,

    /// <summary>
    /// 工作流风格 (如 structured)
    /// </summary>
    WorkflowStyle
}
