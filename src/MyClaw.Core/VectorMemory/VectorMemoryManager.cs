using System.Text;
using System.Text.Json;
using MyClaw.Core.Entities;

namespace MyClaw.Core.VectorMemory;

/// <summary>
/// 向量记忆管理器 - 整合向量存储与记忆系统
/// </summary>
public class VectorMemoryManager
{
    private readonly RagRetriever _retriever;
    private readonly string _workspace;
    private readonly string _vectorStorePath;

    public IVectorStore VectorStore => _retriever.VectorStore;
    public IEmbeddingService EmbeddingService => _retriever.EmbeddingService;
    public RagRetriever Retriever => _retriever;

    public VectorMemoryManager(string workspace, int dimension = 384)
    {
        _workspace = workspace;
        _vectorStorePath = Path.Combine(workspace, "memory", "vectors.json");

        var vectorStore = new InMemoryVectorStore(dimension);
        var embeddingService = new SimpleEmbeddingService(dimension);
        _retriever = new RagRetriever(vectorStore, embeddingService);
    }

    /// <summary>
    /// 初始化 - 加载已有向量索引
    /// </summary>
    public async Task InitializeAsync()
    {
        await _retriever.VectorStore.LoadAsync(_vectorStorePath);
    }

    /// <summary>
    /// 保存向量索引
    /// </summary>
    public async Task SaveAsync()
    {
        await _retriever.VectorStore.SaveAsync(_vectorStorePath);
    }

    /// <summary>
    /// 索引长期记忆 (MEMORY.md)
    /// </summary>
    public async Task<int> IndexLongTermMemoryAsync()
    {
        var memoryPath = Path.Combine(_workspace, "MEMORY.md");
        if (!File.Exists(memoryPath)) return 0;

        var content = await File.ReadAllTextAsync(memoryPath);
        if (string.IsNullOrWhiteSpace(content)) return 0;

        return await _retriever.IndexAsync(content, "long_term", memoryPath, importance: 0.9);
    }

    /// <summary>
    /// 索引每日日志
    /// </summary>
    public async Task<int> IndexDailyLogsAsync(int days = 7)
    {
        var memoryDir = Path.Combine(_workspace, "memory");
        if (!Directory.Exists(memoryDir)) return 0;

        var count = 0;
        var today = DateTime.Now;

        for (int i = 0; i < days; i++)
        {
            var date = today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            var logPath = Path.Combine(memoryDir, $"{dateStr}.md");

            if (File.Exists(logPath))
            {
                var content = await File.ReadAllTextAsync(logPath);
                if (!string.IsNullOrWhiteSpace(content))
                {
                    await _retriever.IndexAsync(
                        content,
                        "daily_log",
                        logPath,
                        importance: 0.7 - (i * 0.05) // 越新越重要
                    );
                    count++;
                }
            }
        }

        return count;
    }

    /// <summary>
    /// 索引实体知识
    /// </summary>
    public async Task<int> IndexEntitiesAsync(IEnumerable<Entity> entities)
    {
        var count = 0;
        foreach (var entity in entities)
        {
            var content = BuildEntityContent(entity);
            if (!string.IsNullOrWhiteSpace(content))
            {
                await _retriever.IndexAsync(
                    content,
                    "entity",
                    metadata: new Dictionary<string, string>
                    {
                        ["entity_type"] = entity.Type.ToString(),
                        ["entity_name"] = entity.Name
                    },
                    importance: 0.6
                );
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 索引技能文件
    /// </summary>
    public async Task<int> IndexSkillsAsync(string skillsDir)
    {
        if (!Directory.Exists(skillsDir)) return 0;

        var count = 0;
        foreach (var skillFile in Directory.GetFiles(skillsDir, "*.md", SearchOption.AllDirectories))
        {
            var content = await File.ReadAllTextAsync(skillFile);
            if (!string.IsNullOrWhiteSpace(content))
            {
                var skillName = Path.GetFileNameWithoutExtension(skillFile);
                await _retriever.IndexAsync(
                    content,
                    "skill",
                    skillFile,
                    importance: 0.5,
                    metadata: new Dictionary<string, string> { ["skill_name"] = skillName }
                );
                count++;
            }
        }
        return count;
    }

    /// <summary>
    /// 语义搜索记忆
    /// </summary>
    public async Task<RagResult> SearchMemoriesAsync(string query, int topK = 5)
    {
        return await _retriever.SearchAsync(query, topK, minScore: 0.2);
    }

    /// <summary>
    /// 获取与查询最相关的上下文
    /// </summary>
    public async Task<string> GetRelevantContextAsync(string query, int maxTokens = 2000)
    {
        return await _retriever.GetRelevantContextAsync(query, maxTokens);
    }

    /// <summary>
    /// 全量重建索引
    /// </summary>
    public async Task<ReindexResult> RebuildIndexAsync(string? skillsDir = null)
    {
        var result = new ReindexResult();

        // 清空现有索引
        await _retriever.VectorStore.ClearAsync();

        // 索引长期记忆
        result.LongTermMemoryEntries = await IndexLongTermMemoryAsync();

        // 索引每日日志
        result.DailyLogFiles = await IndexDailyLogsAsync(30); // 最近30天

        // 索引技能 (如果提供)
        if (!string.IsNullOrEmpty(skillsDir))
        {
            result.SkillFiles = await IndexSkillsAsync(skillsDir);
        }

        // 保存索引
        await SaveAsync();

        result.TotalEntries = _retriever.VectorStore.Count;
        return result;
    }

    /// <summary>
    /// 获取统计信息
    /// </summary>
    public VectorMemoryStats GetStats()
    {
        var entries = (_retriever.VectorStore as InMemoryVectorStore)?.GetAllEntries() ?? Enumerable.Empty<VectorMemoryEntry>();

        return new VectorMemoryStats
        {
            TotalEntries = entries.Count(),
            BySourceType = entries.GroupBy(e => e.SourceType).ToDictionary(g => g.Key, g => g.Count()),
            AverageImportance = entries.Any() ? entries.Average(e => e.Importance) : 0,
            OldestEntry = entries.Any() ? entries.Min(e => e.CreatedAt) : null,
            NewestEntry = entries.Any() ? entries.Max(e => e.CreatedAt) : null
        };
    }

    /// <summary>
    /// 构建实体内容用于索引
    /// </summary>
    private string BuildEntityContent(Entity entity)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"实体: {entity.Name}");
        sb.AppendLine($"类型: {entity.Type}");

        if (entity.Attributes.TryGetValue("description", out var description) && !string.IsNullOrEmpty(description))
        {
            sb.AppendLine($"描述: {description}");
        }

        if (entity.Relations.Count > 0)
        {
            sb.AppendLine("关系:");
            foreach (var rel in entity.Relations)
            {
                sb.AppendLine($"  - {rel}");
            }
        }

        return sb.ToString();
    }
}

/// <summary>
/// 重建索引结果
/// </summary>
public class ReindexResult
{
    public int LongTermMemoryEntries { get; set; }
    public int DailyLogFiles { get; set; }
    public int SkillFiles { get; set; }
    public int TotalEntries { get; set; }
}

/// <summary>
/// 向量记忆统计
/// </summary>
public class VectorMemoryStats
{
    public int TotalEntries { get; set; }
    public Dictionary<string, int> BySourceType { get; set; } = new();
    public double AverageImportance { get; set; }
    public DateTime? OldestEntry { get; set; }
    public DateTime? NewestEntry { get; set; }
}
