namespace MyClaw.Core.Analytics;

/// <summary>
/// 使用统计数据
/// </summary>
public class UsageAnalytics
{
    /// <summary>
    /// 工具调用次数统计
    /// </summary>
    public Dictionary<string, int> ToolCalls { get; set; } = new();

    /// <summary>
    /// 使用的提示词统计
    /// </summary>
    public Dictionary<string, int> PromptsUsed { get; set; } = new();

    /// <summary>
    /// 启动次数
    /// </summary>
    public int BootCount { get; set; }

    /// <summary>
    /// 总启动时间(毫秒)
    /// </summary>
    public long TotalBootMs { get; set; }

    /// <summary>
    /// 最后活动时间
    /// </summary>
    public string LastActivity { get; set; } = string.Empty;

    /// <summary>
    /// 最后一次无聊扫描执行时间
    /// </summary>
    public string LastBoredomExecution { get; set; } = string.Empty;

    /// <summary>
    /// 技能使用统计
    /// </summary>
    public Dictionary<string, int> SkillUsage { get; set; } = new();

    /// <summary>
    /// 每日蒸馏次数
    /// </summary>
    public int DailyDistillations { get; set; }

    /// <summary>
    /// 获取平均启动时间
    /// </summary>
    public long AverageBootMs => BootCount > 0 ? TotalBootMs / BootCount : 0;

    /// <summary>
    /// 获取最常用的工具(前N个)
    /// </summary>
    public List<KeyValuePair<string, int>> GetTopTools(int count = 5)
    {
        return ToolCalls
            .OrderByDescending(x => x.Value)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// 获取总工具调用次数
    /// </summary>
    public int TotalToolCalls => ToolCalls.Values.Sum();

    /// <summary>
    /// 获取总技能使用次数
    /// </summary>
    public int TotalSkillUsage => SkillUsage.Values.Sum();
}

/// <summary>
/// 持久化状态
/// </summary>
public class AnalyticsState
{
    public UsageAnalytics Analytics { get; set; } = new();
    public Dictionary<string, string> PreviousHashes { get; set; } = new();
}
