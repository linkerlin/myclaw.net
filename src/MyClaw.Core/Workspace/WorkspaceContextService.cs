using MyClaw.Core.Ace;

namespace MyClaw.Core.Workspace;

/// <summary>
/// 工作区上下文服务 - 将工作区信息集成到系统提示中
/// </summary>
public class WorkspaceContextService
{
    private readonly WorkspaceDetector _detector;
    private WorkspaceInfo? _cachedInfo;
    private DateTime _cacheTime;
    private readonly TimeSpan _cacheTtl = TimeSpan.FromMinutes(5);

    public WorkspaceContextService(string? workspacePath = null)
    {
        _detector = new WorkspaceDetector(workspacePath);
    }

    /// <summary>
    /// 是否使用紧凑工作区上下文（与 MiniClaw 一致：Project | Path | Git | Stack 单块）
    /// </summary>
    public bool UseCompactWorkspaceContext { get; set; } = true;

    /// <summary>
    /// 获取工作区上下文段落
    /// </summary>
    public async Task<ContextSection> GetContextSectionAsync()
    {
        var info = await GetWorkspaceInfoAsync();
        var content = UseCompactWorkspaceContext
            ? "## 👁️ Workspace\n" + info.ToCompactContextString() + "\n"
            : info.ToContextString();

        return new ContextSection
        {
            Name = "workspace",
            Content = content,
            Priority = 6
        };
    }

    /// <summary>
    /// 获取紧凑格式工作区段落（仅 Project | Path | Git | Stack，与 MiniClaw 一致）
    /// </summary>
    public async Task<ContextSection> GetCompactContextSectionAsync()
    {
        var info = await GetWorkspaceInfoAsync();
        return new ContextSection
        {
            Name = "workspace",
            Content = "## 👁️ Workspace\n" + info.ToCompactContextString() + "\n",
            Priority = 6
        };
    }

    /// <summary>
    /// 获取快速上下文（使用缓存或不执行命令）
    /// </summary>
    public ContextSection GetQuickContextSection()
    {
        var info = _detector.DetectQuick();
        var content = UseCompactWorkspaceContext
            ? "## 👁️ Workspace\n" + info.ToCompactContextString() + "\n"
            : info.ToContextString();

        return new ContextSection
        {
            Name = "workspace",
            Content = content,
            Priority = 6
        };
    }

    /// <summary>
    /// 获取工作区信息（带缓存）
    /// </summary>
    public async Task<WorkspaceInfo> GetWorkspaceInfoAsync()
    {
        if (_cachedInfo != null && DateTime.Now - _cacheTime < _cacheTtl)
        {
            return _cachedInfo;
        }

        _cachedInfo = await _detector.DetectAsync();
        _cacheTime = DateTime.Now;
        return _cachedInfo;
    }

    /// <summary>
    /// 清除缓存
    /// </summary>
    public void InvalidateCache()
    {
        _cachedInfo = null;
    }

    /// <summary>
    /// 获取工作区摘要（用于日志或状态显示）
    /// </summary>
    public async Task<string> GetSummaryAsync()
    {
        var info = await GetWorkspaceInfoAsync();
        var parts = new List<string>
        {
            $"📁 {info.Name}"
        };

        if (info.Git.IsRepo)
        {
            parts.Add($"🌿 {info.Git.Branch}");
            if (info.Git.UncommittedChanges > 0)
            {
                parts.Add($"⚠️ {info.Git.UncommittedChanges} changes");
            }
        }

        if (info.TechStack.Count > 0)
        {
            parts.Add($"🔧 {string.Join(", ", info.TechStack.Take(3))}");
        }

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// 检测工作区是否有未提交的更改
    /// </summary>
    public async Task<bool> HasUncommittedChangesAsync()
    {
        var info = await GetWorkspaceInfoAsync();
        return info.Git.IsRepo && info.Git.UncommittedChanges > 0;
    }

    /// <summary>
    /// 获取主要技术栈
    /// </summary>
    public async Task<List<string>> GetPrimaryTechStackAsync()
    {
        var info = await GetWorkspaceInfoAsync();
        
        // 返回主要技术栈（排除通用的）
        var primaryTechs = info.TechStack
            .Where(t => !IsGenericTech(t))
            .Take(5)
            .ToList();

        return primaryTechs;
    }

    private bool IsGenericTech(string tech)
    {
        // 这些是通用技术，通常不是主要开发语言/框架
        var genericTechs = new[] { "Docker", "Make", "GitHub Actions", "GitLab CI" };
        return genericTechs.Contains(tech);
    }
}
