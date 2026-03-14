namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 向量存储接口
/// </summary>
public interface IVectorStore
{
    /// <summary>
    /// 向量维度
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// 存储条目数量
    /// </summary>
    int Count { get; }

    /// <summary>
    /// 添加或更新条目
    /// </summary>
    Task<string> UpsertAsync(VectorMemoryEntry entry);

    /// <summary>
    /// 批量添加条目
    /// </summary>
    Task<int> UpsertBatchAsync(IEnumerable<VectorMemoryEntry> entries);

    /// <summary>
    /// 获取条目
    /// </summary>
    Task<VectorMemoryEntry?> GetAsync(string id);

    /// <summary>
    /// 删除条目
    /// </summary>
    Task<bool> DeleteAsync(string id);

    /// <summary>
    /// 清空所有条目
    /// </summary>
    Task ClearAsync();

    /// <summary>
    /// 向量相似度搜索
    /// </summary>
    Task<List<VectorSearchResult>> SearchAsync(VectorSearchRequest request);

    /// <summary>
    /// 持久化到文件
    /// </summary>
    Task SaveAsync(string path);

    /// <summary>
    /// 从文件加载
    /// </summary>
    Task LoadAsync(string path);
}
