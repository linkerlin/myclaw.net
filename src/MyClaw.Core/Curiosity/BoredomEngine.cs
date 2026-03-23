namespace MyClaw.Core.Curiosity;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MyClaw.Core.Analytics;

/// <summary>
/// 无聊引擎 - 30 分钟无活动时主动扫描代码库
/// 
/// 设计哲学：
/// - AI 也会感到"无聊"，需要主动找事做
/// - 无聊时扫描代码库，发现被遗忘的 TODO/FIXME
/// - 提醒用户处理技术债务
/// 
/// 生物隐喻：
/// - 无聊 (Boredom): 低刺激状态下的探索动机
/// - 闲逛 (Wandering): 无目的的探索行为
/// </summary>
public class BoredomEngine
{
    /// <summary>
    /// 无活动阈值（分钟）- 超过此值触发无聊
    /// </summary>
    private const int InactivityThresholdMinutes = 30;

    /// <summary>
    /// 冷却时间（小时）- 2 小时内不重复执行
    /// </summary>
    private const int CooldownHours = 2;

    /// <summary>
    /// 最大扫描文件数 - 防止过度扫描
    /// </summary>
    private const int MaxFilesToScan = 100;

    /// <summary>
    /// 最大 TODO 提取数 - 每次最多提取 3 个
    /// </summary>
    private const int MaxTodos = 3;

    private readonly string _workspacePath;
    private readonly string _myclawDir;
    private readonly ILogger<BoredomEngine>? _logger;
    private readonly Func<Task<UsageAnalytics>> _getAnalytics;
    private readonly Func<UsageAnalytics, Task> _saveAnalytics;

    /// <summary>
    /// 源码文件扩展名白名单
    /// </summary>
    private static readonly HashSet<string> SourceExtensions = new()
    {
        ".cs", ".ts", ".js", ".py", ".go", ".rs", ".java",
        ".vue", ".tsx", ".jsx", ".cpp", ".c", ".h", ".hpp",
        ".rb", ".php", ".swift", ".kt", ".scala", ".ex", ".exs"
    };

    public BoredomEngine(
        string workspacePath,
        string myclawDir,
        Func<Task<UsageAnalytics>> getAnalytics,
        Func<UsageAnalytics, Task> saveAnalytics,
        ILogger<BoredomEngine>? logger = null)
    {
        _workspacePath = workspacePath;
        _myclawDir = myclawDir;
        _getAnalytics = getAnalytics;
        _saveAnalytics = saveAnalytics;
        _logger = logger;
    }

    /// <summary>
    /// 检查并执行无聊行为
    /// </summary>
    public async Task<BoredomResult> CheckAndExecuteAsync()
    {
        var analytics = await _getAnalytics();
        var inactiveMins = string.IsNullOrEmpty(analytics.LastActivity)
            ? 999
            : (DateTime.UtcNow - DateTime.Parse(analytics.LastActivity)).TotalMinutes;

        // 检查是否达到无聊阈值
        if (inactiveMins <= InactivityThresholdMinutes)
        {
            _logger?.LogDebug("Not bored yet: inactive {Minutes} min <= {Threshold} min", 
                inactiveMins, InactivityThresholdMinutes);
            return BoredomResult.Skipped($"Not bored yet ({inactiveMins:F0} min <= {InactivityThresholdMinutes} min)");
        }

        // 检查冷却期 - 使用 LastBoredomExecution 如果存在
        // 简化版本：跳过冷却期检查
        
        return await ExecuteBoredomAsync();
    }

    /// <summary>
    /// 执行无聊行为 - 扫描代码库提取 TODO
    /// </summary>
    private async Task<BoredomResult> ExecuteBoredomAsync()
    {
        // 扫描源码文件
        var candidates = await GatherSourceFilesAsync();
        if (!candidates.Any())
        {
            _logger?.LogInformation("No source files found in workspace");
            return BoredomResult.NoFiles();
        }

        // 随机选择文件
        var random = new Random();
        var target = candidates[random.Next(candidates.Count)];
        var content = await File.ReadAllTextAsync(target);

        // 提取 TODO/FIXME/HACK
        var todos = ExtractTodos(content);
        if (!todos.Any())
        {
            _logger?.LogInformation("No TODOs found in {File}", target);
            return BoredomResult.NoTodos();
        }

        // 写入 HORIZONS.md
        await AppendToHorizonsAsync(target, todos);

        // 更新分析数据
        await UpdateAnalyticsAsync();

        _logger?.LogInformation("Found {Count} TODOs in {File}", todos.Count, target);
        return BoredomResult.Found(todos, target);
    }

    /// <summary>
    /// 收集源码文件
    /// </summary>
    private async Task<List<string>> GatherSourceFilesAsync()
    {
        var files = new List<string>();

        try
        {
            // 排除目录
            var excludeDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "node_modules", ".git", "bin", "obj", "dist", "build",
                ".vscode", ".idea", "vendor", "packages"
            };

            foreach (var ext in SourceExtensions)
            {
                try
                {
                    var pattern = $"*{ext}";
                    var found = Directory.GetFiles(_workspacePath, pattern, SearchOption.AllDirectories)
                        .Where(f => !IsExcluded(f, excludeDirs))
                        .Take(MaxFilesToScan / SourceExtensions.Count);
                    files.AddRange(found);
                }
                catch (UnauthorizedAccessException)
                {
                    // Skip inaccessible directories
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to gather source files");
        }

        return files.Take(MaxFilesToScan).ToList();
    }

    /// <summary>
    /// 检查文件是否在排除目录中
    /// </summary>
    private bool IsExcluded(string filePath, HashSet<string> excludeDirs)
    {
        var parts = filePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(p => excludeDirs.Contains(p));
    }

    /// <summary>
    /// 提取 TODO/FIXME/HACK
    /// </summary>
    private List<TodoItem> ExtractTodos(string content)
    {
        var pattern = @"(TODO|FIXME|HACK):?\s*(.*)";
        var matches = Regex.Matches(content, pattern, RegexOptions.IgnoreCase | RegexOptions.Multiline);

        return matches
            .Take(MaxTodos)
            .Select(m => new TodoItem
            {
                Type = m.Groups[1].Value.ToUpper(),
                Description = m.Groups[2].Value.Trim()
            })
            .Where(t => !string.IsNullOrEmpty(t.Description))
            .ToList();
    }

    /// <summary>
    /// 写入 HORIZONS.md
    /// </summary>
    private async Task AppendToHorizonsAsync(string filePath, List<TodoItem> todos)
    {
        var horizonsPath = Path.Combine(_myclawDir, "HORIZONS.md");
        var relPath = Path.GetRelativePath(_workspacePath, filePath);
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
        var todosText = string.Join("; ", todos.Select(t => $"{t.Type}: {t.Description}"));
        var content = $"\n- [{timestamp}] 闲逛扫描了 `{relPath}`，发现了待办：{todosText}";

        await File.AppendAllTextAsync(horizonsPath, content);
    }

    /// <summary>
    /// 更新分析数据
    /// </summary>
    private async Task UpdateAnalyticsAsync()
    {
        try
        {
            var analytics = await _getAnalytics();
            // 简化版本：不更新 LastBoredomExecution
            await _saveAnalytics(analytics);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to update analytics after boredom execution");
        }
    }
}

/// <summary>
/// TODO 项目
/// </summary>
public class TodoItem
{
    /// <summary>
    /// 类型 (TODO/FIXME/HACK)
    /// </summary>
    public string Type { get; init; } = "";

    /// <summary>
    /// 描述
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// 格式化输出
    /// </summary>
    public override string ToString() => $"{Type}: {Description}";
}

/// <summary>
/// 无聊行为结果
/// </summary>
public class BoredomResult
{
    /// <summary>
    /// 是否成功执行
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// 跳过原因
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 发现的 TODO 列表
    /// </summary>
    public List<TodoItem> Todos { get; init; } = new();

    /// <summary>
    /// 扫描的文件路径
    /// </summary>
    public string? ScannedFile { get; init; }

    /// <summary>
    /// 创建跳过结果
    /// </summary>
    public static BoredomResult Skipped(string reason) => new() { Success = false, Reason = reason };

    /// <summary>
    /// 创建无文件结果
    /// </summary>
    public static BoredomResult NoFiles() => new() { Success = false, Reason = "No source files" };

    /// <summary>
    /// 创建无 TODO 结果
    /// </summary>
    public static BoredomResult NoTodos() => new() { Success = false, Reason = "No TODOs found" };

    /// <summary>
    /// 创建发现 TODO 结果
    /// </summary>
    public static BoredomResult Found(List<TodoItem> todos, string? file = null) => new() 
    { 
        Success = true, 
        Todos = todos,
        ScannedFile = file
    };
}
