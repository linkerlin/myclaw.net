using System.Text.Json;

namespace MyClaw.Core.Analytics;

/// <summary>
/// 单次工具调用记录（与改进方案 v3 对齐：支持 success、durationMs）
/// </summary>
public class ToolCallRecord
{
    public string ToolName { get; set; } = "";
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; } = true;
    public int DurationMs { get; set; }
}

/// <summary>
/// 一段时间内的工具使用统计
/// </summary>
public class ToolUsageStats
{
    public int TotalCalls { get; set; }
    public Dictionary<string, int> CallsByTool { get; set; } = new();
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public List<KeyValuePair<string, int>> MostUsedTools { get; set; } = new();
}

/// <summary>
/// 工具调用追踪 - 按次记录并支持按时间段聚合（与 MiniClaw 使用统计对齐）
/// </summary>
public class ToolUsageTracker
{
    private readonly string _workspacePath;
    private readonly string _statsDir;
    private readonly object _lock = new();
    private const string LogFileName = "tool_calls.jsonl";

    public ToolUsageTracker(string workspacePath)
    {
        _workspacePath = workspacePath;
        _statsDir = Path.Combine(workspacePath, ".myclaw", "analytics");
    }

    private string LogFilePath => Path.Combine(_statsDir, LogFileName);

    /// <summary>
    /// 记录一次工具调用
    /// </summary>
    public async Task RecordToolCallAsync(string toolName, bool success = true, int durationMs = 0, CancellationToken ct = default)
    {
        var record = new ToolCallRecord
        {
            ToolName = toolName,
            Timestamp = DateTime.UtcNow,
            Success = success,
            DurationMs = durationMs
        };
        var line = JsonSerializer.Serialize(record) + "\n";
        try
        {
            Directory.CreateDirectory(_statsDir);
            await File.AppendAllTextAsync(LogFilePath, line, ct);
        }
        catch
        {
            // 忽略写入失败
        }
    }

    /// <summary>
    /// 获取指定时间范围内的统计
    /// </summary>
    public ToolUsageStats GetStats(TimeSpan period)
    {
        var since = DateTime.UtcNow - period;
        var records = ReadRecordsSince(since);
        var total = records.Count;
        var successCount = records.Count(r => r.Success);
        var byTool = records.GroupBy(r => r.ToolName).ToDictionary(g => g.Key, g => g.Count());
        var avgDuration = records.Count > 0 ? records.Average(r => r.DurationMs) : 0;
        var mostUsed = byTool.OrderByDescending(x => x.Value).Take(5).ToList();

        return new ToolUsageStats
        {
            TotalCalls = total,
            CallsByTool = byTool,
            SuccessRate = total > 0 ? (double)successCount / total : 0,
            AverageDurationMs = avgDuration,
            MostUsedTools = mostUsed
        };
    }

    /// <summary>
    /// 获取今日 / 昨日 / 本周统计（便捷方法）
    /// </summary>
    public ToolUsageStats GetTodayStats() => GetStats(TimeSpan.FromDays(1));
    public ToolUsageStats GetYesterdayStats() => GetStats(TimeSpan.FromDays(2)); // 含昨天+今天，可再过滤
    public ToolUsageStats GetWeeklyStats() => GetStats(TimeSpan.FromDays(7));

    private List<ToolCallRecord> ReadRecordsSince(DateTime since)
    {
        var list = new List<ToolCallRecord>();
        if (!File.Exists(LogFilePath)) return list;

        lock (_lock)
        {
            try
            {
                foreach (var line in File.ReadLines(LogFilePath))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var record = JsonSerializer.Deserialize<ToolCallRecord>(line);
                        if (record != null && record.Timestamp >= since)
                            list.Add(record);
                    }
                    catch
                    {
                        // 跳过无效行
                    }
                }
            }
            catch
            {
                // 忽略读错误
            }
        }
        return list;
    }
}
