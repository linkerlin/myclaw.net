namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 向量记忆条目
/// </summary>
public class VectorMemoryEntry
{
    /// <summary>
    /// 条目唯一标识
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// 原始文本内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 向量嵌入 (维度由嵌入服务决定)
    /// </summary>
    public float[] Embedding { get; set; } = Array.Empty<float>();

    /// <summary>
    /// 元数据
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// 来源类型 (long_term, daily, entity, skill)
    /// </summary>
    public string SourceType { get; set; } = "unknown";

    /// <summary>
    /// 来源文件路径
    /// </summary>
    public string? SourcePath { get; set; }

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 最后访问时间
    /// </summary>
    public DateTime LastAccessedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// 访问次数
    /// </summary>
    public int AccessCount { get; set; }

    /// <summary>
    /// 重要性分数 (0-1)
    /// </summary>
    public double Importance { get; set; } = 0.5;
}
