using System.Text.Json;

namespace MyClaw.Core.Evolution;

/// <summary>
/// 单次进化记录（与改进方案 v3 进化历史追踪对齐）
/// </summary>
public class EvolutionHistoryEntry
{
    public int Generation { get; set; }
    public DateTime Timestamp { get; set; }
    public int MutationCount { get; set; }
    public string[] PatternTypes { get; set; } = Array.Empty<string>();
}

/// <summary>
/// 进化历史记录 - 持久化每次进化便于报告与可视化
/// </summary>
public class EvolutionHistory
{
    private readonly string _historyPath;
    private readonly object _lock = new();

    public EvolutionHistory(string myclawDir)
    {
        _historyPath = Path.Combine(myclawDir, "evolution-history.jsonl");
    }

    /// <summary>
    /// 记录一次进化
    /// </summary>
    public async Task RecordAsync(int generation, int mutationCount, IEnumerable<DetectedPattern> patterns, CancellationToken ct = default)
    {
        var entry = new EvolutionHistoryEntry
        {
            Generation = generation,
            Timestamp = DateTime.UtcNow,
            MutationCount = mutationCount,
            PatternTypes = patterns.Select(p => p.Type.ToString()).Distinct().ToArray()
        };
        var line = JsonSerializer.Serialize(entry) + "\n";
        try
        {
            var dir = Path.GetDirectoryName(_historyPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            lock (_lock)
            {
                File.AppendAllText(_historyPath, line);
            }
        }
        catch
        {
            // 忽略写入失败
        }
        await Task.CompletedTask;
    }

    /// <summary>
    /// 获取最近 N 次进化记录
    /// </summary>
    public List<EvolutionHistoryEntry> GetRecent(int count = 20)
    {
        var all = GetAll();
        return all.OrderByDescending(e => e.Timestamp).Take(count).ToList();
    }

    /// <summary>
    /// 获取全部记录（按时间升序）
    /// </summary>
    public List<EvolutionHistoryEntry> GetAll()
    {
        var list = new List<EvolutionHistoryEntry>();
        if (!File.Exists(_historyPath)) return list;

        lock (_lock)
        {
            try
            {
                foreach (var line in File.ReadLines(_historyPath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var entry = JsonSerializer.Deserialize<EvolutionHistoryEntry>(line);
                        if (entry != null) list.Add(entry);
                    }
                    catch { /* 跳过无效行 */ }
                }
            }
            catch { }
        }
        return list.OrderBy(e => e.Timestamp).ToList();
    }
}
