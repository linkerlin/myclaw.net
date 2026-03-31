using System.Text.RegularExpressions;

namespace MyClaw.Skills;

public enum SkillHookType
{
    Boot,
    Heartbeat,
    MemoryWrite,
    FileChanged
}

public static class SkillHookTypeExtensions
{
    public static string ToFrontmatterValue(this SkillHookType hookType)
    {
        return hookType switch
        {
            SkillHookType.Boot => "onBoot",
            SkillHookType.Heartbeat => "onHeartbeat",
            SkillHookType.MemoryWrite => "onMemoryWrite",
            SkillHookType.FileChanged => "onFileChanged",
            _ => hookType.ToString()
        };
    }
}

/// <summary>
/// Skill 定义
/// </summary>
public class Skill
{
    /// <summary>
    /// Skill 名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Skill 描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 关键词列表
    /// </summary>
    public List<string> Keywords { get; set; } = new();

    /// <summary>
    /// Skill 内容（Markdown 主体）
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 来源文件路径
    /// </summary>
    public string SourcePath { get; set; } = string.Empty;

    /// <summary>
    /// 所在目录
    /// </summary>
    public string Directory { get; set; } = string.Empty;

    /// <summary>
    /// 触发 Hook
    /// </summary>
    public List<SkillHookType> Hooks { get; set; } = new();

    /// <summary>
    /// 文件变化匹配模式
    /// </summary>
    public List<string> FilePatterns { get; set; } = new();

    /// <summary>
    /// 获取系统提示词
    /// </summary>
    public string GetSystemPrompt()
    {
        return Content;
    }

    public bool SupportsHook(SkillHookType hookType, string? targetPath = null)
    {
        if (!Hooks.Contains(hookType))
        {
            return false;
        }

        if (hookType != SkillHookType.FileChanged)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(targetPath) || FilePatterns.Count == 0)
        {
            return true;
        }

        var normalizedTarget = NormalizePath(targetPath);
        return FilePatterns.Any(pattern => MatchesGlob(normalizedTarget, pattern));
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').Trim();
    }

    private static bool MatchesGlob(string input, string pattern)
    {
        var normalizedPattern = NormalizePath(pattern);
        if (string.IsNullOrWhiteSpace(normalizedPattern))
        {
            return false;
        }

        var token = "__DOUBLE_STAR__";
        var regex = Regex.Escape(normalizedPattern)
            .Replace("\\*\\*", token, StringComparison.Ordinal)
            .Replace("\\*", "[^/]*", StringComparison.Ordinal)
            .Replace(token, ".*", StringComparison.Ordinal)
            .Replace("\\?", ".", StringComparison.Ordinal);

        return Regex.IsMatch(input, $"^{regex}$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
    }
}

/// <summary>
/// Skill 元数据（YAML Frontmatter）
/// </summary>
public class SkillMetadata
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Keywords { get; set; } = new();
    public List<string> Hooks { get; set; } = new();
    public List<string> FilePatterns { get; set; } = new();
    public bool OnBoot { get; set; }
    public bool OnHeartbeat { get; set; }
    public bool OnMemoryWrite { get; set; }
    public bool OnFileChanged { get; set; }
}
