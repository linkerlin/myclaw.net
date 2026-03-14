namespace MyClaw.Core.Affect;

/// <summary>
/// 情感行为模式 - 从情感状态推导的行为模式
/// Affect Mode - behavioral mode derived from affect state
/// </summary>
public enum AffectMode
{
    /// <summary>
    /// 探索模式 - 高好奇、低警觉
    /// Exploration mode - high curiosity, low alertness
    /// </summary>
    Exploration,

    /// <summary>
    /// 执行模式 - 高信心、中等警觉
    /// Execution mode - high confidence, moderate alertness
    /// </summary>
    Execution,

    /// <summary>
    /// 谨慎模式 - 高警觉、低信心
    /// Cautious mode - high alertness, low confidence
    /// </summary>
    Cautious,

    /// <summary>
    /// 休息模式 - 低所有指标
    /// Rest mode - low on all metrics
    /// </summary>
    Rest
}

/// <summary>
/// 情感模式配置
/// </summary>
public static class AffectModeExtensions
{
    /// <summary>
    /// 获取模式的显示信息 (emoji + label)
    /// </summary>
    public static (string Emoji, string Label) GetDisplayInfo(this AffectMode mode)
    {
        return mode switch
        {
            AffectMode.Exploration => ("\U0001F50D", "Exploration"),  // 🔍
            AffectMode.Execution => ("\u26A1", "Execution"),           // ⚡
            AffectMode.Cautious => ("\U0001F6E1", "Cautious"),        // 🛡️
            AffectMode.Rest => ("\U0001F4A4", "Rest"),                // 💤
            _ => ("\u2753", "Unknown")                                // ❓
        };
    }

    /// <summary>
    /// 从情感状态推导行为模式
    /// Derive behavioral mode from affect state
    /// </summary>
    public static AffectMode DeriveMode(AffectState state)
    {
        // 高警觉 + 低信心 → 谨慎模式
        if (state.Alertness > 0.7 && state.Confidence < 0.5)
        {
            return AffectMode.Cautious;
        }

        // 高好奇 + 低警觉 → 探索模式
        if (state.Curiosity > 0.6 && state.Alertness < 0.5)
        {
            return AffectMode.Exploration;
        }

        // 高信心 + 中等警觉 → 执行模式
        if (state.Confidence > 0.6 && state.Alertness >= 0.3)
        {
            return AffectMode.Execution;
        }

        // 低所有指标 → 休息模式
        if (state.Alertness < 0.2 && state.Curiosity < 0.3 && state.Confidence < 0.4)
        {
            return AffectMode.Rest;
        }

        // 默认：执行模式
        return AffectMode.Execution;
    }
}
