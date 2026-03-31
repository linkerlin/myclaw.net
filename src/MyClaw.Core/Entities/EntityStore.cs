using System.Globalization;
using System.Text.Json;

namespace MyClaw.Core.Entities;

/// <summary>
/// 实体知识图谱存储
/// </summary>
public class EntityStore
{
    private const int MaxVitality = 100;
    private const int DailyVitalityDecay = 10;
    private const int MentionRecovery = 25;
    private const int LinkRecovery = 15;
    private readonly string _entitiesFile;
    private readonly List<Entity> _entities = new();
    private bool _loaded = false;
    private readonly object _lock = new();
    private readonly Func<DateTime> _nowProvider;

    public EntityStore(string workspace, Func<DateTime>? nowProvider = null)
    {
        _entitiesFile = Path.Combine(workspace, "entities.json");
        _nowProvider = nowProvider ?? (() => DateTime.Now);
    }

    /// <summary>
    /// 加载实体数据
    /// </summary>
    public async Task LoadAsync()
    {
        if (_loaded) return;

        lock (_lock)
        {
            if (_loaded) return;

            try
            {
                if (File.Exists(_entitiesFile))
                {
                    var json = File.ReadAllText(_entitiesFile);
                    var data = JsonSerializer.Deserialize<EntityData>(json);
                    if (data?.Entities != null)
                    {
                        _entities.Clear();
                        _entities.AddRange(data.Entities);
                    }
                }
            }
            catch { /* ignore load errors */ }

            _loaded = true;
        }

        await Task.CompletedTask;
    }

    /// <summary>
    /// 保存实体数据
    /// </summary>
    public async Task SaveAsync()
    {
        await EnsureCurrentStateAsync();
        Persist();

        await Task.CompletedTask;
    }

    /// <summary>
    /// 添加或更新实体
    /// </summary>
    public async Task<Entity> AddAsync(Entity entity)
    {
        await EnsureCurrentStateAsync();

        var now = _nowProvider().Date;
        var nowText = FormatDate(now);
        var existing = _entities.FirstOrDefault(e =>
            e.Name.Equals(entity.Name, StringComparison.OrdinalIgnoreCase));

        if (existing != null)
        {
            existing.LastMentioned = nowText;
            existing.MentionCount++;
            RestoreVitality(existing, now, MentionRecovery);

            // 合并属性
            foreach (var attr in entity.Attributes)
            {
                existing.Attributes[attr.Key] = attr.Value;
            }

            // 合并关系
            foreach (var rel in entity.Relations)
            {
                if (!existing.Relations.Contains(rel))
                {
                    existing.Relations.Add(rel);
                }
            }

            await SaveAsync();
            return existing;
        }

        // 新实体
        entity.FirstMentioned = nowText;
        entity.LastMentioned = nowText;
        entity.Vitality = MaxVitality;
        entity.VitalityUpdatedAt = nowText;

        lock (_lock)
        {
            _entities.Add(entity);
        }

        await SaveAsync();
        return entity;
    }

    /// <summary>
    /// 删除实体
    /// </summary>
    public async Task<bool> RemoveAsync(string name)
    {
        await EnsureCurrentStateAsync();

        lock (_lock)
        {
            var idx = _entities.FindIndex(e =>
                e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (idx == -1) return false;

            _entities.RemoveAt(idx);
        }

        await SaveAsync();
        return true;
    }

    /// <summary>
    /// 关联实体
    /// </summary>
    public async Task<bool> LinkAsync(string name, string relation)
    {
        await EnsureCurrentStateAsync();

        var now = _nowProvider().Date;
        var nowText = FormatDate(now);

        var entity = _entities.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        if (entity == null) return false;

        if (!entity.Relations.Contains(relation))
        {
            entity.Relations.Add(relation);
            entity.LastMentioned = nowText;
            RestoreVitality(entity, now, LinkRecovery);
            await SaveAsync();
        }

        return true;
    }

    /// <summary>
    /// 查询实体
    /// </summary>
    public async Task<Entity?> QueryAsync(string name)
    {
        await EnsureCurrentStateAsync();

        return _entities.FirstOrDefault(e =>
            e.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 列出实体
    /// </summary>
    public async Task<List<Entity>> ListAsync(EntityType? filterType = null)
    {
        await EnsureCurrentStateAsync();

        if (filterType.HasValue)
        {
            return _entities.Where(e => e.Type == filterType.Value).ToList();
        }

        return _entities.ToList();
    }

    /// <summary>
    /// 获取实体数量
    /// </summary>
    public async Task<int> GetCountAsync()
    {
        await EnsureCurrentStateAsync();
        return _entities.Count;
    }

    /// <summary>
    /// 从文本中提取相关实体
    /// </summary>
    public async Task<List<Entity>> SurfaceRelevantAsync(string text)
    {
        await EnsureCurrentStateAsync();

        if (string.IsNullOrEmpty(text) || _entities.Count == 0)
            return new List<Entity>();

        var lowerText = text.ToLower();

        return _entities
            .Where(e => lowerText.Contains(e.Name.ToLower()))
            .OrderByDescending(e => e.MentionCount)
            .Take(5)
            .ToList();
    }

    private async Task EnsureCurrentStateAsync()
    {
        await LoadAsync();

        if (ApplyDailyLifecycle(_nowProvider().Date))
        {
            Persist();
        }
    }

    private void Persist()
    {
        lock (_lock)
        {
            var data = new EntityData { Entities = _entities };
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_entitiesFile, json);
        }
    }

    private bool ApplyDailyLifecycle(DateTime today)
    {
        lock (_lock)
        {
            var changed = false;

            foreach (var entity in _entities)
            {
                changed |= DecayVitality(entity, today);
            }

            if (_entities.RemoveAll(entity => entity.Vitality <= 0) > 0)
            {
                changed = true;
            }

            return changed;
        }
    }

    private static bool DecayVitality(Entity entity, DateTime today)
    {
        var changed = false;
        var normalizedVitality = Math.Clamp(entity.Vitality, 0, MaxVitality);
        if (normalizedVitality != entity.Vitality)
        {
            entity.Vitality = normalizedVitality;
            changed = true;
        }

        var lastUpdated = ParseDate(entity.VitalityUpdatedAt)
            ?? ParseDate(entity.LastMentioned)
            ?? ParseDate(entity.FirstMentioned)
            ?? today;

        var elapsedDays = (today.Date - lastUpdated.Date).Days;
        if (elapsedDays > 0)
        {
            var decayedVitality = Math.Max(0, entity.Vitality - (elapsedDays * DailyVitalityDecay));
            if (decayedVitality != entity.Vitality)
            {
                entity.Vitality = decayedVitality;
                changed = true;
            }

            var updatedAt = FormatDate(today);
            if (!entity.VitalityUpdatedAt.Equals(updatedAt, StringComparison.Ordinal))
            {
                entity.VitalityUpdatedAt = updatedAt;
                changed = true;
            }
        }
        else if (string.IsNullOrWhiteSpace(entity.VitalityUpdatedAt))
        {
            entity.VitalityUpdatedAt = FormatDate(lastUpdated);
            changed = true;
        }

        return changed;
    }

    private static void RestoreVitality(Entity entity, DateTime now, int recovery)
    {
        entity.Vitality = Math.Min(MaxVitality, Math.Max(0, entity.Vitality) + recovery);
        entity.VitalityUpdatedAt = FormatDate(now);
    }

    private static DateTime? ParseDate(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.Date;
        }

        return null;
    }

    private static string FormatDate(DateTime value)
    {
        return value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private class EntityData
    {
        public List<Entity> Entities { get; set; } = new();
    }
}
