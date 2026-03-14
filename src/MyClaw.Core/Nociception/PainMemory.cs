namespace MyClaw.Core.Nociception;

/// <summary>
/// 痛觉记忆 - 记录负面经验以形成保护性本能
/// Pain Memory - records negative experiences to form protective instincts
/// </summary>
public class PainMemory
{
    /// <summary>
    /// 触发情境 - 什么情况导致了痛觉
    /// Context that caused the pain
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// 执行动作 - 什么动作导致了痛觉
    /// Action that led to the pain
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// 负面后果 - 产生了什么负面结果
    /// Negative consequence
    /// </summary>
    public string Consequence { get; set; } = string.Empty;

    /// <summary>
    /// 痛觉强度 (0-1)
    /// Pain intensity
    /// </summary>
    public double Intensity { get; set; }

    /// <summary>
    /// 记录时间
    /// Timestamp when recorded
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// 当前回避权重 (随时间衰减)
    /// Current avoidance weight (decays over time)
    /// </summary>
    public double Weight { get; set; }

    /// <summary>
    /// 创建副本
    /// </summary>
    public PainMemory Clone()
    {
        return new PainMemory
        {
            Context = Context,
            Action = Action,
            Consequence = Consequence,
            Intensity = Intensity,
            Timestamp = Timestamp,
            Weight = Weight
        };
    }
}
