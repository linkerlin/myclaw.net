using System.Text;

namespace MyClaw.Core.VectorMemory;

/// <summary>
/// RAG 检索结果
/// </summary>
public class RagResult
{
    /// <summary>
    /// 检索到的上下文文本
    /// </summary>
    public string Context { get; set; } = string.Empty;

    /// <summary>
    /// 搜索结果列表
    /// </summary>
    public List<VectorSearchResult> Sources { get; set; } = new();

    /// <summary>
    /// 总耗时 (毫秒)
    /// </summary>
    public long ElapsedMs { get; set; }

    /// <summary>
    /// 查询嵌入耗时 (毫秒)
    /// </summary>
    public long EmbeddingMs { get; set; }

    /// <summary>
    /// 搜索耗时 (毫秒)
    /// </summary>
    public long SearchMs { get; set; }
}

/// <summary>
/// RAG 检索器 - 提供语义搜索和上下文检索功能
/// </summary>
public class RagRetriever
{
    private readonly IVectorStore _vectorStore;
    private readonly IEmbeddingService _embeddingService;
    private readonly RagOptions _options;

    public IVectorStore VectorStore => _vectorStore;
    public IEmbeddingService EmbeddingService => _embeddingService;

    public RagRetriever(IVectorStore vectorStore, IEmbeddingService embeddingService, RagOptions? options = null)
    {
        _vectorStore = vectorStore;
        _embeddingService = embeddingService;
        _options = options ?? new RagOptions();
    }

    /// <summary>
    /// 语义搜索 - 根据查询返回相关记忆
    /// </summary>
    public async Task<RagResult> SearchAsync(string query, int topK = 5, double minScore = 0.3)
    {
        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var result = new RagResult();

        // 1. 嵌入查询
        var embedSw = System.Diagnostics.Stopwatch.StartNew();
        var queryVector = await _embeddingService.EmbedAsync(query);
        embedSw.Stop();
        result.EmbeddingMs = embedSw.ElapsedMilliseconds;

        // 2. 向量搜索
        var searchSw = System.Diagnostics.Stopwatch.StartNew();
        var searchResults = SearchWithVector(queryVector, topK, minScore);
        searchSw.Stop();
        result.SearchMs = searchSw.ElapsedMilliseconds;

        // 3. 构建上下文
        var contextBuilder = new StringBuilder();
        foreach (var searchResult in searchResults)
        {
            var entry = searchResult.Entry;
            contextBuilder.AppendLine($"[{entry.SourceType}] (相关度: {searchResult.Score:F2})");
            contextBuilder.AppendLine(entry.Content);
            contextBuilder.AppendLine();
        }

        result.Sources = searchResults;
        result.Context = contextBuilder.ToString();

        totalSw.Stop();
        result.ElapsedMs = totalSw.ElapsedMilliseconds;

        return result;
    }

    /// <summary>
    /// 使用预计算的向量进行搜索
    /// </summary>
    private List<VectorSearchResult> SearchWithVector(float[] queryVector, int topK, double minScore)
    {
        if (_vectorStore is InMemoryVectorStore memoryStore)
        {
            return memoryStore.SearchWithVector(queryVector, topK, minScore);
        }

        // 通用接口调用
        var request = new VectorSearchRequest
        {
            QueryVector = queryVector,
            TopK = topK,
            MinScore = minScore
        };

        return _vectorStore.SearchAsync(request).GetAwaiter().GetResult();
    }

    /// <summary>
    /// 索引文本到向量存储
    /// </summary>
    /// <returns>索引的条目数量</returns>
    public async Task<int> IndexAsync(string content, string sourceType, string? sourcePath = null, double importance = 0.5, Dictionary<string, string>? metadata = null)
    {
        // 分块处理长文本
        var chunks = ChunkText(content, _options.MaxChunkSize, _options.ChunkOverlap);

        var count = 0;
        foreach (var chunk in chunks)
        {
            if (string.IsNullOrWhiteSpace(chunk)) continue;

            var embedding = await _embeddingService.EmbedAsync(chunk);

            var entry = new VectorMemoryEntry
            {
                Content = chunk,
                Embedding = embedding,
                SourceType = sourceType,
                SourcePath = sourcePath,
                Importance = importance,
                Metadata = metadata ?? new Dictionary<string, string>()
            };

            await _vectorStore.UpsertAsync(entry);
            count++;
        }

        return count;
    }

    /// <summary>
    /// 批量索引文档
    /// </summary>
    public async Task<int> IndexBatchAsync(IEnumerable<DocumentToIndex> documents)
    {
        var count = 0;
        foreach (var doc in documents)
        {
            await IndexAsync(doc.Content, doc.SourceType, doc.SourcePath, doc.Importance, doc.Metadata);
            count++;
        }
        return count;
    }

    /// <summary>
    /// 混合检索 - 结合关键词和语义搜索
    /// </summary>
    public async Task<RagResult> HybridSearchAsync(string query, int topK = 5, double semanticWeight = 0.7)
    {
        var semanticResults = await SearchAsync(query, topK * 2, 0.2);

        // 关键词匹配增强
        var keywords = ExtractKeywords(query);
        var enhancedResults = new List<VectorSearchResult>();

        foreach (var result in semanticResults.Sources)
        {
            var keywordScore = CalculateKeywordScore(result.Entry.Content, keywords);
            var combinedScore = (result.Score * semanticWeight) + (keywordScore * (1 - semanticWeight));

            enhancedResults.Add(new VectorSearchResult
            {
                Entry = result.Entry,
                Score = combinedScore,
                ElapsedMs = result.ElapsedMs
            });
        }

        // 重新排序
        var sortedResults = enhancedResults
            .OrderByDescending(r => r.Score)
            .Take(topK)
            .ToList();

        // 重建上下文
        var contextBuilder = new StringBuilder();
        foreach (var result in sortedResults)
        {
            contextBuilder.AppendLine($"[{result.Entry.SourceType}] (相关度: {result.Score:F2})");
            contextBuilder.AppendLine(result.Entry.Content);
            contextBuilder.AppendLine();
        }

        return new RagResult
        {
            Context = contextBuilder.ToString(),
            Sources = sortedResults,
            ElapsedMs = semanticResults.ElapsedMs,
            EmbeddingMs = semanticResults.EmbeddingMs,
            SearchMs = semanticResults.SearchMs
        };
    }

    /// <summary>
    /// 获取相关记忆上下文 (用于 LLM 提示)
    /// </summary>
    public async Task<string> GetRelevantContextAsync(string query, int maxTokens = 2000)
    {
        var result = await SearchAsync(query, topK: 10, minScore: 0.2);

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("# 相关记忆");
        contextBuilder.AppendLine();

        var currentTokens = 0;
        foreach (var source in result.Sources)
        {
            var estimatedTokens = source.Entry.Content.Length / 4; // 粗略估计

            if (currentTokens + estimatedTokens > maxTokens)
                break;

            contextBuilder.AppendLine($"## {source.Entry.SourceType} (相关度: {source.Score:F2})");
            contextBuilder.AppendLine(source.Entry.Content);
            contextBuilder.AppendLine();

            currentTokens += estimatedTokens;
        }

        return contextBuilder.ToString();
    }

    /// <summary>
    /// 文本分块
    /// </summary>
    private List<string> ChunkText(string text, int maxSize, int overlap)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        if (text.Length <= maxSize)
            return new List<string> { text };

        var chunks = new List<string>();
        var sentences = text.Split(new[] { '。', '.', '！', '!', '？', '?', '\n' },
            StringSplitOptions.RemoveEmptyEntries);

        var currentChunk = new StringBuilder();
        var currentLength = 0;

        foreach (var sentence in sentences)
        {
            var sentenceLength = sentence.Length;

            if (currentLength + sentenceLength > maxSize && currentChunk.Length > 0)
            {
                chunks.Add(currentChunk.ToString().Trim());

                // 处理重叠
                if (overlap > 0 && chunks.Count > 0)
                {
                    var lastChunk = chunks[^1];
                    var overlapStart = Math.Max(0, lastChunk.Length - overlap);
                    currentChunk.Clear();
                    currentChunk.Append(lastChunk.Substring(overlapStart));
                    currentLength = currentChunk.Length;
                }
                else
                {
                    currentChunk.Clear();
                    currentLength = 0;
                }
            }

            currentChunk.Append(sentence);
            currentChunk.Append(' ');
            currentLength += sentenceLength + 1;
        }

        if (currentChunk.Length > 0)
        {
            chunks.Add(currentChunk.ToString().Trim());
        }

        return chunks;
    }

    /// <summary>
    /// 提取关键词
    /// </summary>
    private HashSet<string> ExtractKeywords(string text)
    {
        var stopWords = new HashSet<string>
        {
            "的", "是", "在", "了", "和", "与", "或", "这", "那", "有", "为",
            "the", "a", "an", "is", "are", "was", "were", "be", "been", "being",
            "have", "has", "had", "do", "does", "did", "will", "would", "could",
            "should", "may", "might", "must", "shall", "can", "need", "dare",
            "to", "of", "in", "for", "on", "with", "at", "by", "from", "as",
            "and", "or", "but", "if", "then", "else", "when", "where", "which"
        };

        return text.ToLowerInvariant()
            .Split(new[] { ' ', '\t', '\n', '\r', '.', ',', '!', '?', ';', ':', '(', ')', '[', ']' },
                StringSplitOptions.RemoveEmptyEntries)
            .Where(w => w.Length >= 2 && !stopWords.Contains(w))
            .ToHashSet();
    }

    /// <summary>
    /// 计算关键词匹配分数
    /// </summary>
    private double CalculateKeywordScore(string content, HashSet<string> keywords)
    {
        if (keywords.Count == 0) return 0;

        var contentWords = ExtractKeywords(content);
        var matches = keywords.Intersect(contentWords).Count();

        return (double)matches / keywords.Count;
    }
}

/// <summary>
/// RAG 配置选项
/// </summary>
public class RagOptions
{
    /// <summary>
    /// 最大分块大小 (字符)
    /// </summary>
    public int MaxChunkSize { get; set; } = 500;

    /// <summary>
    /// 分块重叠大小 (字符)
    /// </summary>
    public int ChunkOverlap { get; set; } = 50;

    /// <summary>
    /// 默认检索数量
    /// </summary>
    public int DefaultTopK { get; set; } = 5;

    /// <summary>
    /// 默认最小相似度
    /// </summary>
    public double DefaultMinScore { get; set; } = 0.3;
}

/// <summary>
/// 待索引文档
/// </summary>
public class DocumentToIndex
{
    public string Content { get; set; } = string.Empty;
    public string SourceType { get; set; } = "unknown";
    public string? SourcePath { get; set; }
    public double Importance { get; set; } = 0.5;
    public Dictionary<string, string>? Metadata { get; set; }
}
