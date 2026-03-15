namespace MyClaw.Skills;

/// <summary>
/// Skill 管理器 - 管理已加载的 Skills，带 5s TTL 缓存
/// </summary>
public class SkillManager
{
    private readonly string _skillsDirectory;
    private readonly Dictionary<string, Skill> _skills = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();
    
    // 缓存相关字段
    private DateTime _lastLoadTime = DateTime.MinValue;
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(5);
    private int _cacheHits = 0;
    private int _cacheMisses = 0;

    public SkillManager(string skillsDirectory)
    {
        _skillsDirectory = skillsDirectory;
    }

    /// <summary>
    /// 缓存统计信息
    /// </summary>
    public SkillCacheStats CacheStats
    {
        get
        {
            lock (_lock)
            {
                return new SkillCacheStats
                {
                    LastLoadTime = _lastLoadTime,
                    CacheHits = _cacheHits,
                    CacheMisses = _cacheMisses,
                    CachedSkillsCount = _skills.Count,
                    IsExpired = IsCacheExpired()
                };
            }
        }
    }

    /// <summary>
    /// 已加载的 Skills（自动处理缓存）
    /// </summary>
    public IReadOnlyList<Skill> LoadedSkills
    {
        get
        {
            EnsureSkillsLoaded();
            lock (_lock)
            {
                return _skills.Values.ToList();
            }
        }
    }

    /// <summary>
    /// 检查缓存是否过期
    /// </summary>
    private bool IsCacheExpired()
    {
        return DateTime.Now - _lastLoadTime > CacheTtl;
    }

    /// <summary>
    /// 确保 Skills 已加载（带缓存逻辑）
    /// </summary>
    private void EnsureSkillsLoaded()
    {
        lock (_lock)
        {
            // 如果缓存有效，直接返回
            if (_skills.Count > 0 && !IsCacheExpired())
            {
                _cacheHits++;
                return;
            }

            // 缓存过期或未加载，重新加载
            _cacheMisses++;
            ReloadSkillsUnsafe();
        }
    }

    /// <summary>
    /// 强制重新加载所有 Skills（无锁，调用方需持有锁）
    /// </summary>
    private void ReloadSkillsUnsafe()
    {
        var skills = SkillLoader.LoadSkills(_skillsDirectory);

        _skills.Clear();
        foreach (var skill in skills)
        {
            _skills[skill.Name] = skill;
        }

        _lastLoadTime = DateTime.Now;

        // 只在首次加载或调试时输出
        if (_cacheMisses <= 1)
        {
            Console.WriteLine($"[skills] 从 {_skillsDirectory} 加载了 {_skills.Count} 个技能");
        }
    }

    /// <summary>
    /// 强制重新加载所有 Skills（公开方法）
    /// </summary>
    public void LoadSkills()
    {
        lock (_lock)
        {
            _cacheMisses++;
            ReloadSkillsUnsafe();
        }
    }

    /// <summary>
    /// 获取指定名称的 Skill（带缓存）
    /// </summary>
    public Skill? GetSkill(string name)
    {
        EnsureSkillsLoaded();
        
        lock (_lock)
        {
            return _skills.TryGetValue(name, out var skill) ? skill : null;
        }
    }

    /// <summary>
    /// 根据关键词查找匹配的 Skills（带缓存）
    /// </summary>
    public List<Skill> FindByKeyword(string keyword)
    {
        EnsureSkillsLoaded();
        
        var normalized = keyword.ToLowerInvariant().Trim();
        if (string.IsNullOrEmpty(normalized))
        {
            return new List<Skill>();
        }

        lock (_lock)
        {
            return _skills.Values
                .Where(s => s.Keywords.Any(k => k.Contains(normalized) || normalized.Contains(k)))
                .ToList();
        }
    }

    /// <summary>
    /// 检查 Skill 是否存在（带缓存）
    /// </summary>
    public bool HasSkill(string name)
    {
        EnsureSkillsLoaded();
        
        lock (_lock)
        {
            return _skills.ContainsKey(name);
        }
    }

    /// <summary>
    /// 获取所有 Skill 名称（带缓存）
    /// </summary>
    public List<string> GetSkillNames()
    {
        EnsureSkillsLoaded();
        
        lock (_lock)
        {
            return _skills.Keys.OrderBy(n => n).ToList();
        }
    }

    /// <summary>
    /// 清除缓存（下次访问将重新加载）
    /// </summary>
    public void ClearCache()
    {
        lock (_lock)
        {
            _skills.Clear();
            _lastLoadTime = DateTime.MinValue;
            _cacheHits = 0;
            _cacheMisses = 0;
        }
    }

    /// <summary>
    /// 重置缓存统计信息
    /// </summary>
    public void ResetStats()
    {
        lock (_lock)
        {
            _cacheHits = 0;
            _cacheMisses = 0;
        }
    }
}

/// <summary>
/// Skill 缓存统计信息
/// </summary>
public class SkillCacheStats
{
    /// <summary>
    /// 上次加载时间
    /// </summary>
    public DateTime LastLoadTime { get; set; }
    
    /// <summary>
    /// 缓存命中次数
    /// </summary>
    public int CacheHits { get; set; }
    
    /// <summary>
    /// 缓存未命中次数（重新加载）
    /// </summary>
    public int CacheMisses { get; set; }
    
    /// <summary>
    /// 缓存的技能数量
    /// </summary>
    public int CachedSkillsCount { get; set; }
    
    /// <summary>
    /// 缓存是否已过期
    /// </summary>
    public bool IsExpired { get; set; }
    
    /// <summary>
    /// 缓存命中率
    /// </summary>
    public double HitRate
    {
        get
        {
            var total = CacheHits + CacheMisses;
            return total > 0 ? (double)CacheHits / total : 0;
        }
    }

    public override string ToString()
    {
        return $"Skills: {CachedSkillsCount} | Hits: {CacheHits} | Misses: {CacheMisses} | Rate: {HitRate:P1} | Expired: {IsExpired}";
    }
}
