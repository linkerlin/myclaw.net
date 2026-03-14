using System.Text.Json;
using MyClaw.Core.Affect;

namespace MyClaw.Core.Nociception;

/// <summary>
/// 痛觉记忆管理器 - 记录和管理"绝对不要做"的事情清单
/// Nociception Manager - records and manages "never do" patterns
/// </summary>
public class NociceptionManager
{
    /// <summary>
    /// 痛觉记忆半衰期 (天) - 7天后权重减半
    /// Pain memory half-life in days
    /// </summary>
    public const int PainDecayDays = 7;

    /// <summary>
    /// 触发回避的最小权重阈值
    /// Minimum weight threshold to trigger avoidance
    /// </summary>
    public const double PainThreshold = 0.3;

    /// <summary>
    /// 最大痛觉记忆数量 (循环缓冲区)
    /// Maximum pain memories (circular buffer)
    /// </summary>
    public const int MaxPainMemories = 50;

    private readonly string _stateFilePath;
    private readonly AffectManager? _affectManager;
    private List<PainMemory> _painMemories;
    private readonly object _lock = new();

    public NociceptionManager(AffectManager? affectManager = null, string? stateFilePath = null)
    {
        _affectManager = affectManager;
        _stateFilePath = stateFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".myclaw",
            "pain_memory.json"
        );
        _painMemories = LoadOrCreateMemories();
    }

    /// <summary>
    /// 当前痛觉记忆数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _painMemories.Count;
            }
        }
    }

    /// <summary>
    /// 记录痛觉 - 存储负面经验
    /// Record pain - stores negative experience
    /// </summary>
    /// <param name="context">触发情境</param>
    /// <param name="action">执行动作</param>
    /// <param name="consequence">负面后果</param>
    /// <param name="intensity">痛觉强度 (0-1)</param>
    public void RecordPain(string context, string action, string consequence, double intensity)
    {
        intensity = Math.Clamp(intensity, 0, 1);

        var pain = new PainMemory
        {
            Context = context,
            Action = action,
            Consequence = consequence,
            Intensity = intensity,
            Timestamp = DateTime.UtcNow,
            Weight = intensity
        };

        lock (_lock)
        {
            _painMemories.Add(pain);

            // 循环缓冲区：保留最近 50 条
            if (_painMemories.Count > MaxPainMemories)
            {
                _painMemories = _painMemories.Skip(_painMemories.Count - MaxPainMemories).ToList();
            }
        }

        SaveMemories();

        // 触发情感系统响应
        _affectManager?.ApplyPain(intensity);
    }

    /// <summary>
    /// 检查是否有相关痛觉记忆 (带衰减)
    /// Check for pain memory with exponential decay
    /// </summary>
    /// <param name="context">当前情境</param>
    /// <param name="action">当前动作</param>
    /// <returns>是否存在需要回避的痛觉记忆</returns>
    public bool HasPainMemory(string context, string action)
    {
        lock (_lock)
        {
            foreach (var pain in _painMemories)
            {
                var decayedWeight = CalculateDecayedWeight(pain);
                if (decayedWeight > PainThreshold)
                {
                    // 必须匹配 action 才触发回避
                    var actionMatches = string.Equals(action, pain.Action, StringComparison.OrdinalIgnoreCase) ||
                                        ContainsMatch(action, pain.Action) ||
                                        ContainsMatch(pain.Action, action);

                    if (actionMatches)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 获取痛觉状态摘要 - 用于生命体征监控
    /// Get pain status summary for vitals monitoring
    /// </summary>
    public (int Count, double TotalLoad, List<string> Warnings) GetPainStatus()
    {
        var warnings = new List<string>();
        double totalLoad = 0;
        int activeCount = 0;

        lock (_lock)
        {
            foreach (var pain in _painMemories)
            {
                var weight = CalculateDecayedWeight(pain);
                if (weight > PainThreshold)
                {
                    activeCount++;
                    totalLoad += weight;
                    warnings.Add($"💢 {pain.Action}: {pain.Consequence} (weight: {weight:F2})");
                }
            }
        }

        return (activeCount, totalLoad, warnings);
    }

    /// <summary>
    /// 获取所有痛觉记忆 (带衰减权重)
    /// Get all pain memories with decayed weights
    /// </summary>
    public List<(PainMemory Pain, double DecayedWeight)> GetAllWithDecay()
    {
        var result = new List<(PainMemory, double)>();

        lock (_lock)
        {
            foreach (var pain in _painMemories)
            {
                var decayedWeight = CalculateDecayedWeight(pain);
                result.Add((pain.Clone(), decayedWeight));
            }
        }

        return result.OrderByDescending(x => x.Item2).ToList();
    }

    /// <summary>
    /// 清除已衰减的痛觉记忆
    /// Clear decayed pain memories
    /// </summary>
    public int ClearDecayedMemories()
    {
        int removed;
        lock (_lock)
        {
            var before = _painMemories.Count;
            _painMemories = _painMemories
                .Where(p => CalculateDecayedWeight(p) > PainThreshold * 0.1)
                .ToList();
            removed = before - _painMemories.Count;
        }

        if (removed > 0)
        {
            SaveMemories();
        }

        return removed;
    }

    /// <summary>
    /// 格式化为上下文字符串
    /// Format as context string for ACE
    /// </summary>
    public string FormatForContext()
    {
        var (count, load, warnings) = GetPainStatus();

        if (count == 0)
        {
            return "## NOCICEPTION: ✅ No active pain memories";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## NOCICEPTION: 💢 {count} active pain memories (load: {load:F2})");

        foreach (var warning in warnings.Take(5))
        {
            sb.AppendLine(warning);
        }

        if (warnings.Count > 5)
        {
            sb.AppendLine($"... and {warnings.Count - 5} more");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 计算衰减后的权重
    /// </summary>
    private double CalculateDecayedWeight(PainMemory pain)
    {
        var daysSince = (DateTime.UtcNow - pain.Timestamp).TotalDays;
        // 指数衰减: weight * 0.5^(days / half_life)
        return pain.Weight * Math.Pow(0.5, daysSince / PainDecayDays);
    }

    /// <summary>
    /// 检查字符串包含关系 (双向)
    /// </summary>
    private static bool ContainsMatch(string a, string b)
    {
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return false;
        return a.Contains(b, StringComparison.OrdinalIgnoreCase);
    }

    private List<PainMemory> LoadOrCreateMemories()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var memories = JsonSerializer.Deserialize<List<PainMemory>>(json);
                return memories ?? new List<PainMemory>();
            }
        }
        catch
        {
            // 加载失败，使用空列表
        }
        return new List<PainMemory>();
    }

    private void SaveMemories()
    {
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            List<PainMemory> toSave;
            lock (_lock)
            {
                toSave = _painMemories.ToList();
            }

            var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_stateFilePath, json);
        }
        catch
        {
            // 保存失败，忽略
        }
    }
}
