namespace MyClaw.Core.Affect;

/// <summary>
/// 统一情感状态 - 所有系统 (痛觉、甲基化、好奇心) 在此汇聚
/// Unified Affect State - all systems converge here
/// </summary>
public class AffectState
{
    /// <summary>
    /// 警觉度 (0-1) - 受痛觉/错误影响
    /// Alertness level - affected by pain/errors
    /// </summary>
    public double Alertness { get; set; }

    /// <summary>
    /// 情绪效价 (-1 to 1) - 受成功/失败比影响
    /// Mood valence - affected by success/failure ratio
    /// </summary>
    public double Mood { get; set; }

    /// <summary>
    /// 好奇驱动力 (0-1) - 受未探索能力影响
    /// Curiosity drive - affected by unexplored capabilities
    /// </summary>
    public double Curiosity { get; set; }

    /// <summary>
    /// 行动信心 (0-1) - 受预测准确度影响
    /// Action confidence - affected by prediction accuracy
    /// </summary>
    public double Confidence { get; set; }

    /// <summary>
    /// 最后更新时间
    /// Last update timestamp
    /// </summary>
    public DateTime LastUpdate { get; set; }

    /// <summary>
    /// 创建默认情感状态 (基线)
    /// </summary>
    public static AffectState CreateDefault()
    {
        return new AffectState
        {
            Alertness = 0.3,
            Mood = 0.5,
            Curiosity = 0.5,
            Confidence = 0.7,
            LastUpdate = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 创建副本
    /// </summary>
    public AffectState Clone()
    {
        return new AffectState
        {
            Alertness = Alertness,
            Mood = Mood,
            Curiosity = Curiosity,
            Confidence = Confidence,
            LastUpdate = LastUpdate
        };
    }
}
