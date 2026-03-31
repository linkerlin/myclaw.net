using System.Security.Cryptography;
using System.Text;
using MyClaw.Core.Affect;

namespace MyClaw.Core.Ace;

/// <summary>
/// 上下文段落
/// </summary>
public class ContextSection
{
    /// <summary>
    /// 段落名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 优先级 (1-10, 越高越重要)
    /// </summary>
    public int Priority { get; set; } = 5;
}

/// <summary>
/// 编译结果
/// </summary>
public class CompiledContext
{
    /// <summary>
    /// 输出内容
    /// </summary>
    public string Output { get; set; } = string.Empty;

    /// <summary>
    /// 总字符数
    /// </summary>
    public int TotalChars { get; set; }

    /// <summary>
    /// Token数估算
    /// </summary>
    public int TotalTokens { get; set; }

    /// <summary>
    /// Token预算
    /// </summary>
    public int BudgetTokens { get; set; }

    /// <summary>
    /// 利用率百分比
    /// </summary>
    public int UtilizationPct { get; set; }

    /// <summary>
    /// 被截断的段落
    /// </summary>
    public List<string> TruncatedSections { get; set; } = new();
}

/// <summary>
/// 内容哈希
/// </summary>
public class ContentHash
{
    public string SectionName { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
}

/// <summary>
/// 上下文编译器 - 管理Token预算
/// </summary>
public class ContextCompiler
{
    private const int CharsPerToken = 4;
    private const int MinimumSkeletonChars = 80;
    private readonly int _tokenBudget;
    private readonly AffectManager? _affectManager;

    public ContextCompiler(int tokenBudget = 8000, AffectManager? affectManager = null)
    {
        _tokenBudget = tokenBudget;
        _affectManager = affectManager;
    }

    /// <summary>
    /// 编译上下文 (带情感状态)
    /// </summary>
    public CompiledContext Compile(List<ContextSection> sections, bool includeAffect = true)
    {
        var maxChars = _tokenBudget * CharsPerToken;
        var output = new StringBuilder();
        var totalChars = 0;
        var truncatedSections = new List<string>();

        // 添加情感状态段落到开头 (高优先级)
        if (includeAffect && _affectManager != null)
        {
            var affectContent = _affectManager.FormatForContext() + "\n---\n";
            output.Append(affectContent);
            totalChars += affectContent.Length;
        }

        var sorted = sections.OrderByDescending(s => s.Priority).ToList();

        foreach (var section in sorted)
        {
            var sectionChars = section.Content.Length;

            if (totalChars + sectionChars <= maxChars)
            {
                output.Append(section.Content);
                totalChars += sectionChars;
            }
            else
            {
                var remaining = maxChars - totalChars;
                var skeleton = CreateSkeleton(section, remaining);
                if (!string.IsNullOrWhiteSpace(skeleton))
                {
                    output.Append(skeleton);
                    totalChars += skeleton.Length;
                    truncatedSections.Add(section.Name);
                }
                else
                {
                    truncatedSections.Add(section.Name);
                }
            }
        }

        var totalTokens = totalChars / CharsPerToken;

        return new CompiledContext
        {
            Output = output.ToString(),
            TotalChars = totalChars,
            TotalTokens = totalTokens,
            BudgetTokens = _tokenBudget,
            UtilizationPct = (int)((double)totalTokens / _tokenBudget * 100),
            TruncatedSections = truncatedSections
        };
    }

    /// <summary>
    /// 计算内容哈希
    /// </summary>
    public string HashString(string content)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(hash).Substring(0, 8).ToLower();
    }

    /// <summary>
    /// 检测内容变化
    /// </summary>
    public (List<string> Changed, List<string> Unchanged, List<string> New) DetectChanges(
        List<ContentHash> current, Dictionary<string, string> previous)
    {
        var changed = new List<string>();
        var unchanged = new List<string>();
        var newSections = new List<string>();

        foreach (var hash in current)
        {
            if (!previous.ContainsKey(hash.SectionName))
            {
                newSections.Add(hash.SectionName);
            }
            else if (previous[hash.SectionName] != hash.Hash)
            {
                changed.Add(hash.SectionName);
            }
            else
            {
                unchanged.Add(hash.SectionName);
            }
        }

        return (changed, unchanged, newSections);
    }

    private static string? CreateSkeleton(ContextSection section, int availableChars)
    {
        if (availableChars < MinimumSkeletonChars)
        {
            return null;
        }

        var normalized = NormalizeLineEndings(section.Content).TrimEnd();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return null;
        }

        var (frontmatter, body) = SplitFrontmatter(normalized);
        var headings = ExtractHeadingLines(body);
        var leadLines = headings.Count == 0 ? ExtractLeadLines(body, 4) : new List<string>();
        var tailLines = ExtractTailLines(body, 6);

        var frontmatterVariants = new[]
        {
            frontmatter,
            TrimBlockLines(frontmatter, 6, "... [frontmatter skeletonized]"),
            string.Empty
        };

        var outlineVariants = new[]
        {
            headings,
            TakeFirst(headings, 4),
            TakeFirst(headings, 2),
            TakeFirst(headings, 1),
            leadLines,
            TakeFirst(leadLines, 2),
            new List<string>()
        };

        var tailVariants = new[]
        {
            tailLines,
            TakeLast(tailLines, 4),
            TakeLast(tailLines, 2),
            new List<string>()
        };

        foreach (var frontmatterCandidate in frontmatterVariants)
        {
            foreach (var outlineCandidate in outlineVariants)
            {
                foreach (var tailCandidate in tailVariants)
                {
                    var candidate = BuildSkeleton(section.Name, normalized.Length, frontmatterCandidate, outlineCandidate, tailCandidate);
                    if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= availableChars)
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    private static string BuildSkeleton(string sectionName, int originalLength, string frontmatter, List<string> outlineLines, List<string> tailLines)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(frontmatter))
        {
            parts.Add(frontmatter.TrimEnd());
        }

        if (outlineLines.Count > 0)
        {
            parts.Add(string.Join("\n", outlineLines));
        }

        var uniqueTail = tailLines
            .Where(line => !outlineLines.Contains(line))
            .ToList();

        var previewLength = parts.Sum(part => part.Length) + uniqueTail.Sum(line => line.Length) + uniqueTail.Count;
        var omittedChars = Math.Max(0, originalLength - previewLength);
        parts.Add($"... [{sectionName}: 已骨架化，保留 frontmatter/标题/尾部上下文，省略约 {omittedChars} 字符]");

        if (uniqueTail.Count > 0)
        {
            parts.Add(string.Join("\n", uniqueTail));
        }

        return string.Join("\n\n", parts.Where(part => !string.IsNullOrWhiteSpace(part))).TrimEnd() + "\n\n";
    }

    private static (string Frontmatter, string Body) SplitFrontmatter(string content)
    {
        if (!content.StartsWith("---\n", StringComparison.Ordinal))
        {
            return (string.Empty, content);
        }

        var lines = content.Split('\n');
        for (var index = 1; index < lines.Length; index++)
        {
            if (lines[index].Trim() == "---")
            {
                var frontmatter = string.Join("\n", lines[..(index + 1)]).TrimEnd();
                var body = string.Join("\n", lines[(index + 1)..]).TrimStart('\n');
                return (frontmatter, body);
            }
        }

        return (string.Empty, content);
    }

    private static List<string> ExtractHeadingLines(string content)
    {
        return content
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => line.StartsWith('#'))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static List<string> ExtractLeadLines(string content, int maxLines)
    {
        return content
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Take(maxLines)
            .ToList();
    }

    private static List<string> ExtractTailLines(string content, int maxLines)
    {
        var lines = content
            .Split('\n')
            .Select(line => line.TrimEnd())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count <= maxLines)
        {
            return lines;
        }

        return lines[^maxLines..];
    }

    private static string TrimBlockLines(string block, int maxLines, string suffix)
    {
        if (string.IsNullOrWhiteSpace(block))
        {
            return string.Empty;
        }

        var lines = block.Split('\n');
        if (lines.Length <= maxLines)
        {
            return block.TrimEnd();
        }

        return string.Join("\n", lines[..maxLines]) + $"\n{suffix}";
    }

    private static List<string> TakeFirst(List<string> lines, int count)
    {
        if (lines.Count <= count)
        {
            return lines;
        }

        return lines.Take(count).ToList();
    }

    private static List<string> TakeLast(List<string> lines, int count)
    {
        if (lines.Count <= count)
        {
            return lines;
        }

        return lines[^count..];
    }

    private static string NormalizeLineEndings(string content)
    {
        return content
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\r", "\n", StringComparison.Ordinal);
    }
}
