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
                if (remaining > 200)
                {
                    var truncated = section.Content.Substring(0, remaining - 50) +
                        $"\n\n... [{section.Name}: 已截断，省略 {sectionChars - remaining} 字符]\n";
                    output.Append(truncated);
                    totalChars += truncated.Length;
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
}
