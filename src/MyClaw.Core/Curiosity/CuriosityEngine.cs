using System.Text.Json;
using MyClaw.Core.Affect;

namespace MyClaw.Core.Curiosity;

/// <summary>
/// 好奇心引擎 - 生成探索目标，驱动主动学习
/// Curiosity Engine - Generate exploration targets, drive proactive learning
/// </summary>
public class CuriosityEngine
{
    private readonly string _myclawDir;
    private readonly string _stateFilePath;
    private List<ExplorationTarget> _targets = new();
    private DateTime _lastGeneration = DateTime.MinValue;

    /// <summary>
    /// 好奇心配置常量
    /// </summary>
    public static class Constants
    {
        /// <summary>
        /// 最小好奇心阈值 (低于此值不生成探索目标)
        /// </summary>
        public const double MinCuriosityThreshold = 0.3;

        /// <summary>
        /// 最大待处理目标数
        /// </summary>
        public const int MaxPendingTargets = 10;

        /// <summary>
        /// 目标过期天数
        /// </summary>
        public const int TargetExpirationDays = 7;

        /// <summary>
        /// 生成间隔（小时）
        /// </summary>
        public const int GenerationIntervalHours = 4;

        /// <summary>
        /// 基础探索优先级
        /// </summary>
        public const double BaseExplorationPriority = 0.5;
    }

    /// <summary>
    /// 创建好奇心引擎实例
    /// </summary>
    /// <param name="myclawDir">MyClaw 数据目录</param>
    public CuriosityEngine(string myclawDir)
    {
        _myclawDir = myclawDir;
        _stateFilePath = Path.Combine(myclawDir, "curiosity_state.json");
        LoadState();
    }

    /// <summary>
    /// 生成探索目标
    /// </summary>
    /// <param name="affect">当前情感状态</param>
    /// <param name="analytics">使用分析数据</param>
    /// <returns>新创建的探索目标列表</returns>
    public List<ExplorationTarget> GenerateTargets(AffectState affect, Dictionary<string, object>? analytics = null)
    {
        var newTargets = new List<ExplorationTarget>();

        // 检查生成间隔
        if (DateTime.UtcNow - _lastGeneration < TimeSpan.FromHours(Constants.GenerationIntervalHours))
        {
            return newTargets;
        }

        // 好奇心调制
        var modulatedCuriosity = ModulateByAffect(affect.Curiosity, affect);
        if (modulatedCuriosity < Constants.MinCuriosityThreshold)
        {
            return newTargets;
        }

        // 根据情感模式生成不同类型的目标
        var mode = AffectModeExtensions.DeriveMode(affect);

        // 清理过期目标
        CleanupExpiredTargets();

        // 生成新目标
        if (mode == AffectMode.Exploration || affect.Curiosity > 0.6)
        {
            // 新概念探索
            if (analytics != null && analytics.TryGetValue("newConcepts", out var concepts) && concepts is List<string> conceptList)
            {
                foreach (var concept in conceptList.Take(3))
                {
                    var target = CreateTarget(ExplorationType.NewConcept, concept, "在对话中发现新概念");
                    if (target != null && !IsDuplicateTarget(target))
                    {
                        newTargets.Add(target);
                        _targets.Add(target);
                    }
                }
            }

            // 未使用工具探索
            if (analytics != null && analytics.TryGetValue("unusedTools", out var tools) && tools is List<string> toolList)
            {
                foreach (var tool in toolList.Take(2))
                {
                    var target = CreateTarget(ExplorationType.UnusedTool, tool, "工具长时间未使用");
                    if (target != null && !IsDuplicateTarget(target))
                    {
                        newTargets.Add(target);
                        _targets.Add(target);
                    }
                }
            }
        }

        // 模式异常探索（谨慎模式也执行）
        if (analytics != null && analytics.TryGetValue("patternAnomalies", out var anomalies) && anomalies is List<string> anomalyList)
        {
            foreach (var anomaly in anomalyList.Take(2))
            {
                var target = CreateTarget(ExplorationType.PatternAnomaly, anomaly, "检测到异常模式");
                if (target != null && !IsDuplicateTarget(target))
                {
                    newTargets.Add(target);
                    _targets.Add(target);
                }
            }
        }

        // 限制待处理目标数
        var pendingCount = _targets.Count(t => t.Status == ExplorationStatus.Pending);
        if (pendingCount > Constants.MaxPendingTargets)
        {
            // 移除最低优先级的待处理目标
            var toRemove = _targets
                .Where(t => t.Status == ExplorationStatus.Pending)
                .OrderBy(t => t.Priority)
                .Take(pendingCount - Constants.MaxPendingTargets)
                .ToList();

            foreach (var target in toRemove)
            {
                target.Status = ExplorationStatus.Abandoned;
            }
        }

        if (newTargets.Count > 0)
        {
            _lastGeneration = DateTime.UtcNow;
            SaveState();
        }

        return newTargets;
    }

    /// <summary>
    /// 创建探索目标
    /// </summary>
    private ExplorationTarget? CreateTarget(ExplorationType type, string topic, string reason)
    {
        if (string.IsNullOrWhiteSpace(topic)) return null;

        return new ExplorationTarget
        {
            Type = type,
            Topic = topic,
            Reason = reason,
            Priority = CalculatePriority(type, topic),
            Status = ExplorationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// 计算探索优先级
    /// </summary>
    private double CalculatePriority(ExplorationType type, string topic)
    {
        var basePriority = type switch
        {
            ExplorationType.UserSuggestion => 0.9,      // 用户建议最高优先
            ExplorationType.PatternAnomaly => 0.8,      // 模式异常高优先
            ExplorationType.NewConcept => 0.7,          // 新概念
            ExplorationType.UnknownDomain => 0.6,       // 未知领域
            ExplorationType.NewFileType => 0.5,         // 新文件类型
            ExplorationType.UnusedTool => 0.4,          // 未使用工具
            _ => Constants.BaseExplorationPriority
        };

        // 根据话题长度/复杂度微调
        var complexityBonus = Math.Min(topic.Length / 100.0, 0.1);

        return Math.Min(1.0, basePriority + complexityBonus);
    }

    /// <summary>
    /// 计算好奇心得分（受情感状态调制）
    /// </summary>
    public double CalculateCuriosityScore(string topic, Dictionary<string, object> analytics)
    {
        if (string.IsNullOrWhiteSpace(topic)) return 0;

        var baseScore = Constants.BaseExplorationPriority;

        // 根据分析数据调整
        if (analytics.TryGetValue("topicFrequency", out var freq) && freq is double frequency)
        {
            // 低频话题更值得探索
            baseScore += (1 - frequency) * 0.3;
        }

        if (analytics.TryGetValue("recentMentions", out var mentions) && mentions is int mentionCount)
        {
            // 最近多次提及的话题更值得探索
            baseScore += Math.Min(mentionCount * 0.05, 0.2);
        }

        return Math.Min(1.0, baseScore);
    }

    /// <summary>
    /// 根据情感状态调制好奇心
    /// </summary>
    public double ModulateByAffect(double baseCuriosity, AffectState affect)
    {
        var mode = AffectModeExtensions.DeriveMode(affect);

        var multiplier = mode switch
        {
            AffectMode.Exploration => 1.3,    // 探索模式增强好奇心
            AffectMode.Execution => 0.8,      // 执行模式降低好奇心
            AffectMode.Cautious => 0.5,       // 谨慎模式大幅降低
            AffectMode.Rest => 0.3,           // 休息模式最低
            _ => 1.0
        };

        // 心情也影响好奇心
        var moodModifier = affect.Mood > 0 ? 1.1 : 0.9;

        return Math.Clamp(baseCuriosity * multiplier * moodModifier, 0, 1);
    }

    /// <summary>
    /// 检查是否为重复目标
    /// </summary>
    private bool IsDuplicateTarget(ExplorationTarget target)
    {
        return _targets.Any(t =>
            t.Status != ExplorationStatus.Abandoned &&
            t.Type == target.Type &&
            string.Equals(t.Topic, target.Topic, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 清理过期目标
    /// </summary>
    private void CleanupExpiredTargets()
    {
        var cutoff = DateTime.UtcNow.AddDays(-Constants.TargetExpirationDays);
        var expired = _targets
            .Where(t => t.Status == ExplorationStatus.Pending && t.CreatedAt < cutoff)
            .ToList();

        foreach (var target in expired)
        {
            target.Status = ExplorationStatus.Abandoned;
            target.UpdatedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// 获取所有待处理目标
    /// </summary>
    public List<ExplorationTarget> GetPendingTargets()
    {
        return _targets
            .Where(t => t.Status == ExplorationStatus.Pending)
            .OrderByDescending(t => t.Priority)
            .ToList();
    }

    /// <summary>
    /// 获取所有目标
    /// </summary>
    public List<ExplorationTarget> GetAllTargets()
    {
        return _targets.ToList();
    }

    /// <summary>
    /// 更新目标状态
    /// </summary>
    public bool UpdateTargetStatus(string targetId, ExplorationStatus status)
    {
        var target = _targets.FirstOrDefault(t => t.Id == targetId);
        if (target == null) return false;

        target.Status = status;
        target.UpdatedAt = DateTime.UtcNow;
        SaveState();
        return true;
    }

    /// <summary>
    /// 添加用户建议的探索目标
    /// </summary>
    public ExplorationTarget AddUserSuggestion(string topic, string reason)
    {
        var target = new ExplorationTarget
        {
            Type = ExplorationType.UserSuggestion,
            Topic = topic,
            Reason = reason,
            Priority = 0.9, // 用户建议高优先
            Status = ExplorationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _targets.Add(target);
        SaveState();
        return target;
    }

    /// <summary>
    /// 获取下一个应该探索的目标
    /// </summary>
    public ExplorationTarget? GetNextTarget(AffectState affect)
    {
        var modulatedCuriosity = ModulateByAffect(affect.Curiosity, affect);
        if (modulatedCuriosity < Constants.MinCuriosityThreshold) return null;

        return GetPendingTargets().FirstOrDefault();
    }

    /// <summary>
    /// 获取好奇心状态摘要
    /// </summary>
    public CuriosityState GetState()
    {
        return new CuriosityState
        {
            TotalTargets = _targets.Count,
            PendingTargets = _targets.Count(t => t.Status == ExplorationStatus.Pending),
            InProgressTargets = _targets.Count(t => t.Status == ExplorationStatus.InProgress),
            CompletedTargets = _targets.Count(t => t.Status == ExplorationStatus.Completed),
            AbandonedTargets = _targets.Count(t => t.Status == ExplorationStatus.Abandoned),
            LastGeneration = _lastGeneration
        };
    }

    /// <summary>
    /// 加载状态
    /// </summary>
    private void LoadState()
    {
        try
        {
            if (File.Exists(_stateFilePath))
            {
                var json = File.ReadAllText(_stateFilePath);
                var state = JsonSerializer.Deserialize<CuriosityStateData>(json);
                if (state != null)
                {
                    _targets = state.Targets ?? new List<ExplorationTarget>();
                    _lastGeneration = state.LastGeneration;
                }
            }
        }
        catch
        {
            _targets = new List<ExplorationTarget>();
        }
    }

    /// <summary>
    /// 保存状态
    /// </summary>
    private void SaveState()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
            var state = new CuriosityStateData
            {
                Targets = _targets,
                LastGeneration = _lastGeneration
            };
            var json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_stateFilePath, json);
        }
        catch
        {
            // 忽略保存错误
        }
    }

    /// <summary>
    /// 内部状态数据结构
    /// </summary>
    private class CuriosityStateData
    {
        public List<ExplorationTarget> Targets { get; set; } = new();
        public DateTime LastGeneration { get; set; }
    }
}

/// <summary>
/// 好奇心状态摘要
/// </summary>
public class CuriosityState
{
    public int TotalTargets { get; set; }
    public int PendingTargets { get; set; }
    public int InProgressTargets { get; set; }
    public int CompletedTargets { get; set; }
    public int AbandonedTargets { get; set; }
    public DateTime LastGeneration { get; set; }

    public override string ToString()
    {
        return $"探索目标: {TotalTargets} 总计, {PendingTargets} 待处理, {CompletedTargets} 已完成";
    }
}
