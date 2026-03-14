namespace MyClaw.Core.Curiosity;

/// <summary>
/// 探索目标状态
/// </summary>
public enum ExplorationStatus
{
    Pending,
    InProgress,
    Completed,
    Abandoned
}

/// <summary>
/// 探索目标类型
/// </summary>
public enum ExplorationType
{
    NewConcept,          // 新概念
    UnusedTool,          // 长时间未使用的工具
    UnknownDomain,       // 用户提到的未知领域
    NewFileType,         // 工作区新文件类型
    PatternAnomaly,      // 模式异常
    UserSuggestion       // 用户建议探索
}

/// <summary>
/// 探索目标
/// </summary>
public class ExplorationTarget
{
    /// <summary>
    /// 目标 ID
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 目标类型
    /// </summary>
    public ExplorationType Type { get; set; }

    /// <summary>
    /// 探索主题/话题
    /// </summary>
    public string Topic { get; set; } = string.Empty;

    /// <summary>
    /// 探索原因
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 优先级 (0-1)
    /// </summary>
    public double Priority { get; set; }

    /// <summary>
    /// 当前状态
    /// </summary>
    public ExplorationStatus Status { get; set; } = ExplorationStatus.Pending;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 上下文数据
    /// </summary>
    public Dictionary<string, object> Context { get; set; } = new();

    /// <summary>
    /// 相关文件路径
    /// </summary>
    public List<string> RelatedFiles { get; set; } = new();
}
