namespace MyClaw.Core.Workspace;

/// <summary>
/// 工作区信息 - 包含项目类型、Git 和技术栈信息（与 MiniClaw 工作区感知对齐）
/// </summary>
public class WorkspaceInfo
{
    /// <summary>
    /// 项目名称 (当前目录名)
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工作区完整路径
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// 检测到的项目类型（React/Vue/Node/Python/Go/DotNet 等）
    /// </summary>
    public ProjectTypeInfo DetectedProjectType { get; set; } = new();

    /// <summary>
    /// Git 仓库信息
    /// </summary>
    public GitInfo Git { get; set; } = new();

    /// <summary>
    /// 检测到的技术栈列表
    /// </summary>
    public List<string> TechStack { get; set; } = new();

    /// <summary>
    /// 检测时间
    /// </summary>
    public DateTime DetectedAt { get; set; }

    /// <summary>
    /// 紧凑格式上下文（与 MiniClaw 一致）：Project: name | Path: ... \n Git: branch | dirty (+N files) \n Stack: ...
    /// </summary>
    public string ToCompactContextString()
    {
        var lines = new List<string>
        {
            $"Project: {Name} | Path: {Path}"
        };
        if (Git.IsRepo)
        {
            var gitLine = $"Git: {Git.Branch}";
            if (Git.UncommittedChanges > 0)
                gitLine += $" | dirty (+{Git.UncommittedChanges} files)";
            lines.Add(gitLine);
        }
        if (TechStack.Count > 0)
            lines.Add($"Stack: {string.Join(", ", TechStack.Take(5))}");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 将工作区信息格式化为上下文字符串（详细版）
    /// </summary>
    public string ToContextString()
    {
        var lines = new List<string>
        {
            "## 👁️ Workspace Awareness",
            $"**Project**: {Name}",
            $"**Path**: `{Path}`"
        };
        if (DetectedProjectType.Type != ProjectType.Unknown)
            lines.Add($"**Type**: {DetectedProjectType.Name} (confidence: {DetectedProjectType.Confidence:F2})");

        if (Git.IsRepo)
        {
            lines.Add($"**Git**: {Git.Branch} | {Git.Status}");
            if (!string.IsNullOrEmpty(Git.RecentCommits))
            {
                lines.Add($"**Recent Commits**:");
                foreach (var line in Git.RecentCommits.Split('\n', StringSplitOptions.RemoveEmptyEntries))
                {
                    lines.Add($"  {line}");
                }
            }
            if (Git.UncommittedChanges > 0)
            {
                lines.Add($"⚠️ **{Git.UncommittedChanges} uncommitted changes**");
            }
        }

        if (TechStack.Count > 0)
        {
            lines.Add($"**Stack**: {string.Join(", ", TechStack)}");
        }

        lines.Add("");
        return string.Join("\n", lines);
    }
}
