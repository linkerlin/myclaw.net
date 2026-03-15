namespace MyClaw.Core.Briefing;

/// <summary>
/// 每日简报模板（与 MiniClaw / 改进方案 v3 对齐）
/// </summary>
public static class BriefingTemplate
{
    public const string Title = "## 📊 昨日回顾 (Daily Briefing)";
    public const string SectionTools = "### 🛠️ 工具使用";
    public const string SectionMemory = "### 🧠 新增记忆";
    public const string SectionFiles = "### 📁 活跃文件";
    public const string SectionTodo = "### ✅ 待办提醒";
    public const string SectionSuggestions = "### 🎯 今日建议";

    /// <summary>
    /// 组装完整简报标题与日期
    /// </summary>
    public static string FormatTitle(string date) => $"## 📊 昨日回顾 (Daily Briefing) — {date}";

    /// <summary>
    /// 工具使用块占位（由 StatisticsReporter / DailyBriefingService 填充）
    /// </summary>
    public static string FormatToolSection(string content) => SectionTools + "\n\n" + content + "\n";

    /// <summary>
    /// 待办提醒块
    /// </summary>
    public static string FormatTodoSection(string[] items)
    {
        if (items.Length == 0) return "";
        var lines = new List<string> { SectionTodo, "" };
        foreach (var item in items)
            lines.Add($"- [ ] {item}");
        lines.Add("");
        return string.Join("\n", lines);
    }

    /// <summary>
    /// 今日建议块
    /// </summary>
    public static string FormatSuggestionsSection(string[] suggestions)
    {
        if (suggestions.Length == 0) return "";
        var lines = new List<string> { SectionSuggestions, "基于你的使用模式，建议:", "" };
        for (var i = 0; i < suggestions.Length; i++)
            lines.Add($"{i + 1}. {suggestions[i]}");
        lines.Add("");
        return string.Join("\n", lines);
    }
}
