namespace MyClaw.Core.Ribosome;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

/// <summary>
/// 核糖体修剪器 - 用进废退机制
/// 50 次心跳后从未使用的工具会被剔除（永生工具除外）
/// 
/// 设计哲学：
/// - 器官退化是生物进化的重要机制
/// - 长期不使用的工具会占用上下文预算
/// - 永生工具是核心本能，永不剔除
/// </summary>
public class RibosomePruner
{
    /// <summary>
    /// 永生工具列表 - 永远不会被剔除
    /// 这些是核心本能工具，即使用户从不使用也保留
    /// </summary>
    private static readonly HashSet<string> ImmortalTools = new()
    {
        "myclaw_exec",      // 感官与手
        "myclaw_read",      // 全脑唤醒
        "myclaw_dream",     // 做梦 - 意义蒸馏
        "myclaw_mutate",    // 基因重写手术
        "myclaw_reproduce", // 孢子繁衍
    };

    /// <summary>
    /// 最小心跳次数 - 低于此值不执行修剪（胚胎期保护）
    /// </summary>
    private const int MinBootCount = 50;

    private readonly string _myclawDir;
    private readonly ILogger<RibosomePruner>? _logger;

    public RibosomePruner(string myclawDir, ILogger<RibosomePruner>? logger = null)
    {
        _myclawDir = myclawDir;
        _logger = logger;
    }

    /// <summary>
    /// 执行核糖体修剪
    /// </summary>
    /// <param name="bootCount">启动次数</param>
    /// <param name="toolCalls">工具调用统计</param>
    /// <returns>修剪结果</returns>
    public async Task<PruneResult> PruneAsync(long bootCount, Dictionary<string, int> toolCalls)
    {
        // 胚胎期不修剪
        if (bootCount < MinBootCount)
        {
            _logger?.LogInformation("Embryonic stage: boot count {BootCount} < {MinBootCount}, skipping prune", 
                bootCount, MinBootCount);
            return PruneResult.CreateSkipped($"Embryonic stage (boot count: {bootCount} < {MinBootCount})");
        }

        var ribosome = await LoadRibosomeAsync();
        var prunedTools = new List<string>();
        var originalToolCount = ribosome.Tools.Count;

        ribosome.Tools = ribosome.Tools.Where(t =>
        {
            // 永生工具保留
            if (ImmortalTools.Contains(t.Name))
                return true;

            // 从未使用的工具剔除
            var calls = toolCalls.GetValueOrDefault(t.Name, 0);
            if (calls == 0)
            {
                prunedTools.Add(t.Name);
                _logger?.LogInformation("Pruning unused tool: {ToolName}", t.Name);
                return false;
            }

            return true;
        }).ToList();

        if (prunedTools.Any())
        {
            // 记录退化日志
            await AppendToHeartbeatAsync(prunedTools, bootCount);
            await SaveRibosomeAsync(ribosome);

            _logger?.LogInformation("Pruned {Count} tools: {Tools}", 
                prunedTools.Count, string.Join(", ", prunedTools));
        }

        return new PruneResult
        {
            PrunedTools = prunedTools,
            OriginalToolCount = originalToolCount,
            RemainingToolCount = ribosome.Tools.Count
        };
    }

    /// <summary>
    /// 记录退化日志到 HEARTBEAT.md
    /// </summary>
    private async Task AppendToHeartbeatAsync(List<string> prunedTools, long bootCount)
    {
        var heartbeatPath = Path.Combine(_myclawDir, "HEARTBEAT.md");
        var content = $"\n> 🧬 [器官退化] 宿主历经{bootCount}次心跳从未使用过：{string.Join(", ", prunedTools)}";
        
        try
        {
            await File.AppendAllTextAsync(heartbeatPath, content);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Failed to append to HEARTBEAT.md");
        }
    }

    /// <summary>
    /// 加载核糖体定义
    /// </summary>
    private async Task<RibosomeDefinition> LoadRibosomeAsync()
    {
        var ribosomePath = Path.Combine(_myclawDir, "RIBOSOME.json");
        
        if (!File.Exists(ribosomePath))
        {
            _logger?.LogWarning("RIBOSOME.json not found at {Path}", ribosomePath);
            return new RibosomeDefinition { Tools = new List<ToolDefinition>() };
        }

        var json = await File.ReadAllTextAsync(ribosomePath);
        var ribosome = JsonSerializer.Deserialize<RibosomeDefinition>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return ribosome ?? new RibosomeDefinition { Tools = new List<ToolDefinition>() };
    }

    /// <summary>
    /// 保存核糖体定义
    /// </summary>
    private async Task SaveRibosomeAsync(RibosomeDefinition ribosome)
    {
        var ribosomePath = Path.Combine(_myclawDir, "RIBOSOME.json");
        var json = JsonSerializer.Serialize(ribosome, new JsonSerializerOptions 
        { 
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(ribosomePath, json);
    }
}

/// <summary>
/// 修剪结果
/// </summary>
public class PruneResult
{
    /// <summary>
    /// 是否跳过修剪
    /// </summary>
    public bool Skipped { get; init; }

    /// <summary>
    /// 跳过原因
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// 被剔除的工具列表
    /// </summary>
    public List<string> PrunedTools { get; init; } = new();

    /// <summary>
    /// 原始工具数量
    /// </summary>
    public int OriginalToolCount { get; init; }

    /// <summary>
    /// 剩余工具数量
    /// </summary>
    public int RemainingToolCount { get; init; }

    /// <summary>
    /// 创建跳过结果
    /// </summary>
    public static PruneResult CreateSkipped(string reason) => new() { Skipped = true, Reason = reason };
}

/// <summary>
/// 核糖体定义
/// </summary>
public class RibosomeDefinition
{
    public string Version { get; set; } = "1.0";
    public string Description { get; set; } = "";
    public List<ToolDefinition> Tools { get; set; } = new();
}

/// <summary>
/// 工具定义
/// </summary>
public class ToolDefinition
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Handler { get; set; } = "";
}
