namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 嵌入服务接口 - 将文本转换为向量
/// </summary>
public interface IEmbeddingService
{
    /// <summary>
    /// 向量维度
    /// </summary>
    int Dimension { get; }

    /// <summary>
    /// 生成文本嵌入向量
    /// </summary>
    Task<float[]> EmbedAsync(string text);

    /// <summary>
    /// 批量生成嵌入向量
    /// </summary>
    Task<List<float[]>> EmbedBatchAsync(IEnumerable<string> texts);
}
