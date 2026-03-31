using System.Text.Json;

namespace MyClaw.Core.Analytics;

/// <summary>
/// 分析服务 - 跟踪使用统计
/// </summary>
public class AnalyticsService
{
    private readonly string _stateFilePath;
    private AnalyticsState _state;
    private bool _loaded = false;
    private readonly object _lock = new();

    public AnalyticsService(string workspacePath)
    {
        _stateFilePath = Path.Combine(workspacePath, "analytics.json");
        _state = new AnalyticsState();
    }

    /// <summary>
    /// 加载状态
    /// </summary>
    private void Load()
    {
        if (_loaded) return;

        lock (_lock)
        {
            if (_loaded) return;

            try
            {
                if (File.Exists(_stateFilePath))
                {
                    var json = File.ReadAllText(_stateFilePath);
                    var loaded = JsonSerializer.Deserialize<AnalyticsState>(json);
                    if (loaded != null)
                    {
                        _state = loaded;
                    }
                }
            }
            catch
            {
                // 使用默认状态
            }

            _loaded = true;
        }
    }

    /// <summary>
    /// 保存状态
    /// </summary>
    private void Save()
    {
        lock (_lock)
        {
            try
            {
                var dir = Path.GetDirectoryName(_stateFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                var json = JsonSerializer.Serialize(_state, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(_stateFilePath, json);
            }
            catch
            {
                // 忽略保存错误
            }
        }
    }

    /// <summary>
    /// 跟踪工具调用
    /// </summary>
    public void TrackToolCall(string toolName)
    {
        Load();
        
        lock (_lock)
        {
            if (!_state.Analytics.ToolCalls.ContainsKey(toolName))
            {
                _state.Analytics.ToolCalls[toolName] = 0;
            }
            _state.Analytics.ToolCalls[toolName]++;
            _state.Analytics.LastActivity = DateTime.UtcNow.ToString("O");
        }
        
        Save();
    }

    /// <summary>
    /// 跟踪提示词使用
    /// </summary>
    public void TrackPrompt(string promptName)
    {
        Load();
        
        lock (_lock)
        {
            if (!_state.Analytics.PromptsUsed.ContainsKey(promptName))
            {
                _state.Analytics.PromptsUsed[promptName] = 0;
            }
            _state.Analytics.PromptsUsed[promptName]++;
            _state.Analytics.LastActivity = DateTime.UtcNow.ToString("O");
        }
        
        Save();
    }

    /// <summary>
    /// 跟踪技能使用
    /// </summary>
    public void TrackSkillUsage(string skillName)
    {
        Load();
        
        lock (_lock)
        {
            if (!_state.Analytics.SkillUsage.ContainsKey(skillName))
            {
                _state.Analytics.SkillUsage[skillName] = 0;
            }
            _state.Analytics.SkillUsage[skillName]++;
            _state.Analytics.LastActivity = DateTime.UtcNow.ToString("O");
        }
        
        Save();
    }

    /// <summary>
    /// 跟踪启动
    /// </summary>
    public void TrackBoot(long bootTimeMs)
    {
        Load();
        
        lock (_lock)
        {
            _state.Analytics.BootCount++;
            _state.Analytics.TotalBootMs += bootTimeMs;
            _state.Analytics.LastActivity = DateTime.UtcNow.ToString("O");
        }
        
        Save();
    }

    /// <summary>
    /// 跟踪蒸馏
    /// </summary>
    public void TrackDistillation()
    {
        Load();
        
        lock (_lock)
        {
            _state.Analytics.DailyDistillations++;
            _state.Analytics.LastActivity = DateTime.UtcNow.ToString("O");
        }
        
        Save();
    }

    /// <summary>
    /// 记录一次无聊执行
    /// </summary>
    public void TrackBoredomExecution(DateTime? executedAt = null)
    {
        Load();

        lock (_lock)
        {
            var now = executedAt ?? DateTime.UtcNow;
            _state.Analytics.LastBoredomExecution = now.ToString("O");
            _state.Analytics.LastActivity = now.ToString("O");
        }

        Save();
    }

    /// <summary>
    /// 获取分析数据
    /// </summary>
    public UsageAnalytics GetAnalytics()
    {
        Load();
        
        lock (_lock)
        {
            // 返回副本
            return new UsageAnalytics
            {
                ToolCalls = new Dictionary<string, int>(_state.Analytics.ToolCalls),
                PromptsUsed = new Dictionary<string, int>(_state.Analytics.PromptsUsed),
                BootCount = _state.Analytics.BootCount,
                TotalBootMs = _state.Analytics.TotalBootMs,
                LastActivity = _state.Analytics.LastActivity,
                LastBoredomExecution = _state.Analytics.LastBoredomExecution,
                SkillUsage = new Dictionary<string, int>(_state.Analytics.SkillUsage),
                DailyDistillations = _state.Analytics.DailyDistillations
            };
        }
    }

    /// <summary>
    /// 更新内容哈希（用于变化检测）
    /// </summary>
    public void UpdateHash(string sectionName, string hash)
    {
        Load();
        
        lock (_lock)
        {
            _state.PreviousHashes[sectionName] = hash;
        }
        
        Save();
    }

    /// <summary>
    /// 获取内容哈希
    /// </summary>
    public string? GetHash(string sectionName)
    {
        Load();
        
        lock (_lock)
        {
            return _state.PreviousHashes.TryGetValue(sectionName, out var hash) ? hash : null;
        }
    }

    /// <summary>
    /// 检测内容变化
    /// </summary>
    public (List<string> Changed, List<string> Unchanged, List<string> New) DetectChanges(
        Dictionary<string, string> currentHashes)
    {
        Load();
        
        var changed = new List<string>();
        var unchanged = new List<string>();
        var newSections = new List<string>();

        lock (_lock)
        {
            foreach (var (name, hash) in currentHashes)
            {
                if (!_state.PreviousHashes.ContainsKey(name))
                {
                    newSections.Add(name);
                }
                else if (_state.PreviousHashes[name] != hash)
                {
                    changed.Add(name);
                }
                else
                {
                    unchanged.Add(name);
                }
            }
        }

        return (changed, unchanged, newSections);
    }

    /// <summary>
    /// 重置每日统计
    /// </summary>
    public void ResetDailyStats()
    {
        Load();
        
        lock (_lock)
        {
            _state.Analytics.DailyDistillations = 0;
        }
        
        Save();
    }
}
