namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 向量搜索结果
/// </summary>
public class VectorSearchResult
{
    /// <summary>
    /// 匹配的条目
    /// </summary>
    public VectorMemoryEntry Entry { get; set; } = new();

    /// <summary>
    /// 相似度分数 (0-1, 越高越相似)
    /// </summary>
    public double Score { get; set; }

    /// <summary>
    /// 搜索耗时 (毫秒)
    /// </summary>
    public long ElapsedMs { get; set; }
}

/// <summary>
/// 向量搜索请求
/// </summary>
public class VectorSearchRequest
{
    /// <summary>
    /// 查询文本 (会被转换为向量)
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// 查询向量 (如果已计算)
    /// </summary>
    public float[]? QueryVector { get; set; }

    /// <summary>
    /// 返回结果数量
    /// </summary>
    public int TopK { get; set; } = 5;

    /// <summary>
    /// 最小相似度阈值
    /// </summary>
    public double MinScore { get; set; } = 0.0;

    /// <summary>
    /// 过滤来源类型
    /// </summary>
    public List<string>? SourceTypes { get; set; }

    /// <summary>
    /// 元数据过滤条件
    /// </summary>
    public Dictionary<string, string>? MetadataFilter { get; set; }
}
