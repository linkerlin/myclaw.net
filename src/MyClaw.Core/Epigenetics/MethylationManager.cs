using System.Text.Json;

namespace MyClaw.Core.Epigenetics;

/// <summary>
/// 甲基化管理器 - 管理半永久性行为适应
/// Methylation Manager - manages semi-permanent behavioral adaptations
/// </summary>
public class MethylationManager
{
    /// <summary>
    /// 触发甲基化的最小模式重复次数
    /// Minimum pattern repetitions to trigger methylation
    /// </summary>
    public const int MethylationThreshold = 10;

    /// <summary>
    /// 模式最小年龄 (天) - 确保模式稳定
    /// Minimum pattern age in days for stability
    /// </summary>
    public const int MethylationAgeDays = 7;

    /// <summary>
    /// 甲基化冷却期 (小时) - 防止过度修改
    /// Cooldown hours between SOUL modifications
    /// </summary>
    public const int MethylationCooldownHours = 48;

    private readonly string _stateFilePath;
    private List<MethylatedTrait> _traits;
    private DateTime _lastMethylationTime;
    private readonly object _lock = new();

    public MethylationManager(string? stateFilePath = null)
    {
        _stateFilePath = stateFilePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".myclaw",
            "methylation.json"
        );
        (_traits, _lastMethylationTime) = LoadOrCreateTraits();
    }

    /// <summary>
    /// 当前甲基化特征数量
    /// </summary>
    public int Count
    {
        get
        {
            lock (_lock)
            {
                return _traits.Count;
            }
        }
    }

    /// <summary>
    /// 最后甲基化时间
    /// </summary>
    public DateTime LastMethylationTime => _lastMethylationTime;

    /// <summary>
    /// 检查模式是否应该触发甲基化
    /// Check if pattern should trigger methylation
    /// </summary>
    /// <param name="patternType">模式类型</param>
    /// <param name="confidence">置信度</param>
    /// <param name="mergedCount">合并次数</param>
    /// <param name="description">模式描述</param>
    /// <returns>甲基化检查结果</returns>
    public (bool Should, string? Trait, string? Value) ShouldMethylate(
        string patternType,
        double confidence,
        int mergedCount,
        string description)
    {
        // 必须高置信度且重复足够次数
        if (confidence < 0.8 || mergedCount < MethylationThreshold)
        {
            return (false, null, null);
        }

        // 检查冷却期
        var hoursSinceLast = (DateTime.UtcNow - _lastMethylationTime).TotalHours;
        if (hoursSinceLast < MethylationCooldownHours)
        {
            return (false, null, null);
        }

        // 从模式描述提取特征和值
        string? trait = null;
        string? value = null;

        if (patternType == "preference" && description.Contains("tool", StringComparison.OrdinalIgnoreCase))
        {
            trait = "interaction_style";
            value = description.Contains("update", StringComparison.OrdinalIgnoreCase)
                ? "proactive_modifier"
                : "active_reader";
        }
        else if (patternType == "temporal")
        {
            trait = "activity_pattern";
            value = "time_sensitive";
        }
        else if (patternType == "workflow")
        {
            trait = "workflow_style";
            value = "structured";
        }

        if (string.IsNullOrEmpty(trait) || string.IsNullOrEmpty(value))
        {
            return (false, null, null);
        }

        // 检查是否已经有高稳定性的相同特征
        lock (_lock)
        {
            var existing = _traits.FirstOrDefault(t => t.Trait == trait);
            if (existing != null && existing.Stability > 0.7)
            {
                return (false, null, null); // 已经稳定
            }
        }

        return (true, trait, value);
    }

    /// <summary>
    /// 应用甲基化 - 将特征更新到系统
    /// Apply methylation - update trait to system
    /// </summary>
    public bool MethylateTrait(
        string trait,
        string value,
        string source,
        int patternCount)
    {
        // 检查冷却期
        var hoursSinceLast = (DateTime.UtcNow - _lastMethylationTime).TotalHours;
        if (hoursSinceLast < MethylationCooldownHours)
        {
            return false;
        }

        // 计算稳定性: 0.5 + count * 0.05, 最大 0.95
        var stability = Math.Min(0.95, 0.5 + patternCount * 0.05);

        var newTrait = new MethylatedTrait
        {
            Trait = trait,
            Value = value,
            Source = source,
            Timestamp = DateTime.UtcNow,
            PatternCount = patternCount,
            Stability = stability
        };

        lock (_lock)
        {
            // 更新或添加特征
            var existingIndex = _traits.FindIndex(t => t.Trait == trait);
            if (existingIndex >= 0)
            {
                _traits[existingIndex] = newTrait;
            }
            else
            {
                _traits.Add(newTrait);
            }
            _lastMethylationTime = DateTime.UtcNow;
        }

        SaveTraits();
        return true;
    }

    /// <summary>
    /// 获取所有甲基化特征
    /// Get all methylated traits
    /// </summary>
    public List<MethylatedTrait> GetMethylatedTraits()
    {
        lock (_lock)
        {
            return _traits.Select(t => t.Clone()).ToList();
        }
    }

    /// <summary>
    /// 获取指定特征的值
    /// Get trait value by name
    /// </summary>
    public string? GetTraitValue(string traitName)
    {
        lock (_lock)
        {
            var trait = _traits.FirstOrDefault(t => t.Trait == traitName);
            return trait?.Value;
        }
    }

    /// <summary>
    /// 移除特征
    /// Remove a trait
    /// </summary>
    public bool RemoveTrait(string traitName)
    {
        bool removed;
        lock (_lock)
        {
            var count = _traits.Count;
            _traits = _traits.Where(t => t.Trait != traitName).ToList();
            removed = _traits.Count < count;
        }

        if (removed)
        {
            SaveTraits();
        }
        return removed;
    }

    /// <summary>
    /// 格式化为上下文字符串
    /// Format as context string for ACE
    /// </summary>
    public string FormatForContext()
    {
        List<MethylatedTrait> traits;
        lock (_lock)
        {
            traits = _traits.ToList();
        }

        if (traits.Count == 0)
        {
            return "## METHYLATION: No methylated traits yet";
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"## METHYLATION: {traits.Count} methylated traits");

        foreach (var trait in traits.OrderByDescending(t => t.Stability))
        {
            sb.AppendLine($"- {trait.Trait}: {trait.Value} (stability: {trait.Stability:P0})");
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// 格式化为 SOUL.md 注释
    /// Format as SOUL.md comment
    /// </summary>
    public string FormatAsSoulComment(MethylatedTrait trait)
    {
        return $"\n<!-- [METHYLATED] {trait.Trait}: {trait.Value} (stability: {(int)(trait.Stability * 100)}%) -->";
    }

    /// <summary>
    /// 获取剩余冷却时间 (小时)
    /// Get remaining cooldown hours
    /// </summary>
    public double GetRemainingCooldownHours()
    {
        var hoursSinceLast = (DateTime.UtcNow - _lastMethylationTime).TotalHours;
        return Math.Max(0, MethylationCooldownHours - hoursSinceLast);
    }

    private (List<MethylatedTrait>, DateTime) LoadOrCreateTraits()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var data = JsonSerializer.Deserialize<MethylationData>(json);
                if (data != null)
                {
                    return (data.Traits ?? new List<MethylatedTrait>(), data.LastMethylationTime);
                }
            }
        }
        catch
        {
            // 加载失败，使用默认值
        }
        return (new List<MethylatedTrait>(), DateTime.MinValue);
    }

    private void SaveTraits()
    {
        try
        {
            var directory = Path.GetDirectoryName(_stateFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            MethylationData data;
            lock (_lock)
            {
                data = new MethylationData
                {
                    Traits = _traits.ToList(),
                    LastMethylationTime = _lastMethylationTime
                };
            }

            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions
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

    private class MethylationData
    {
        public List<MethylatedTrait>? Traits { get; set; }
        public DateTime LastMethylationTime { get; set; }
    }
}
