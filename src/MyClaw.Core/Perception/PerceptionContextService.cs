using MyClaw.Core.Ace;

namespace MyClaw.Core.Perception;

/// <summary>
/// 平台感知上下文服务 - 将平台感知信息集成到系统提示中
/// </summary>
public class PerceptionContextService
{
    private readonly IPerceptionProvider _provider;
    private readonly TimeSpan _cacheTtl;
    private PerceptionSnapshot? _cachedSnapshot;
    private DateTime _cacheTime;

    public PerceptionContextService(IPerceptionProvider? provider = null, TimeSpan? cacheTtl = null)
    {
        _provider = provider ?? PerceptionProviderFactory.CreateDefault();
        _cacheTtl = cacheTtl ?? TimeSpan.FromMinutes(2);
    }

    public async Task<ContextSection?> GetContextSectionAsync()
    {
        var snapshot = await GetSnapshotAsync();
        return CreateSection(snapshot);
    }

    public ContextSection? GetQuickContextSection()
    {
        var snapshot = IsCacheValid()
            ? _cachedSnapshot
            : _provider.CaptureAsync().GetAwaiter().GetResult();

        if (snapshot == null)
        {
            return null;
        }

        _cachedSnapshot ??= snapshot;
        if (!IsCacheValid())
        {
            _cachedSnapshot = snapshot;
            _cacheTime = DateTime.UtcNow;
        }

        return CreateSection(snapshot);
    }

    public async Task<PerceptionSnapshot> GetSnapshotAsync()
    {
        if (IsCacheValid())
        {
            return _cachedSnapshot!;
        }

        _cachedSnapshot = await _provider.CaptureAsync();
        _cacheTime = DateTime.UtcNow;
        return _cachedSnapshot;
    }

    public void InvalidateCache()
    {
        _cachedSnapshot = null;
    }

    private bool IsCacheValid()
    {
        return _cachedSnapshot != null && DateTime.UtcNow - _cacheTime < _cacheTtl;
    }

    private static ContextSection? CreateSection(PerceptionSnapshot snapshot)
    {
        var content = snapshot.ToContextString();
        if (string.IsNullOrWhiteSpace(content))
        {
            return null;
        }

        return new ContextSection
        {
            Name = "perception",
            Content = content + "\n",
            Priority = Math.Clamp(snapshot.PriorityHint, 1, 10)
        };
    }
}