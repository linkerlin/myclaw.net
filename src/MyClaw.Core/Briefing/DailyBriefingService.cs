using MyClaw.Core.Analytics;
using MyClaw.Core.Entities;
using MyClaw.Core.Memory;

namespace MyClaw.Core.Briefing;

/// <summary>
/// 每日简报服务 - 生成昨日回顾和今日概览（与改进方案 v3 / MiniClaw 对齐）
/// </summary>
public class DailyBriefingService
{
    private readonly MemoryStore _memoryStore;
    private readonly AnalyticsService _analyticsService;
    private readonly EntityStore? _entityStore;
    private readonly StatisticsReporter? _statisticsReporter;
    private readonly ToolUsageTracker? _toolTracker;

    public DailyBriefingService(
        MemoryStore memoryStore,
        AnalyticsService analyticsService,
        EntityStore? entityStore = null,
        StatisticsReporter? statisticsReporter = null,
        ToolUsageTracker? toolTracker = null)
    {
        _memoryStore = memoryStore;
        _analyticsService = analyticsService;
        _entityStore = entityStore;
        _statisticsReporter = statisticsReporter;
        _toolTracker = toolTracker;
    }

    /// <summary>
    /// 生成每日简报
    /// </summary>
    public async Task<string> GenerateBriefingAsync()
    {
        var now = DateTime.Now;
        var today = now.ToString("yyyy-MM-dd");

        var lines = new List<string>
        {
            BriefingTemplate.FormatTitle(today),
            ""
        };

        // 🛠️ 工具使用（优先使用 StatisticsReporter 的格式）
        var statsSection = GenerateStatsSection();
        lines.Add(statsSection);

        // 🧠 新增记忆 / 实体摘要
        var entitySection = await GenerateEntitySectionAsync();
        if (!string.IsNullOrEmpty(entitySection))
            lines.Add(entitySection);

        // 📁 活跃文件（可选，从记忆或 Git 可扩展）
        var filesSection = GenerateActiveFilesSection();
        if (!string.IsNullOrEmpty(filesSection))
            lines.Add(filesSection);

        // ✅ 待办提醒
        var todoSection = await GenerateTodoSectionAsync();
        if (!string.IsNullOrEmpty(todoSection))
            lines.Add(todoSection);

        // 🎯 今日建议
        var suggestionsSection = GenerateSuggestionsSection();
        if (!string.IsNullOrEmpty(suggestionsSection))
            lines.Add(suggestionsSection);

        // 昨日活动
        var yesterdaySection = await GenerateYesterdaySectionAsync(now.AddDays(-1).ToString("yyyy-MM-dd"));
        if (!string.IsNullOrEmpty(yesterdaySection))
            lines.Add(yesterdaySection);

        // 未解决问题
        var openQuestionsSection = await GenerateOpenQuestionsSectionAsync(now.AddDays(-1).ToString("yyyy-MM-dd"));
        if (!string.IsNullOrEmpty(openQuestionsSection))
            lines.Add(openQuestionsSection);

        // 健康检查
        var healthSection = GenerateHealthSection();
        if (!string.IsNullOrEmpty(healthSection))
            lines.Add(healthSection);

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成昨日回顾部分
    /// </summary>
    private async Task<string> GenerateYesterdaySectionAsync(string yesterday)
    {
        var yesterdayLog = _memoryStore.GetRecentMemories(2);
        
        if (string.IsNullOrWhiteSpace(yesterdayLog))
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### 📋 Yesterday's Activity",
            ""
        };

        // 解析条目数
        var entries = yesterdayLog.Split('\n')
            .Where(l => l.TrimStart().StartsWith("- ["))
            .ToList();

        lines.Add($"Total entries: {entries.Count}");
        lines.Add("");

        // 显示最近 5 条
        var recent = entries.TakeLast(5).ToList();
        if (recent.Count > 0)
        {
            lines.Add("Recent entries:");
            foreach (var entry in recent)
            {
                lines.Add(entry);
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成未解决问题部分
    /// </summary>
    private async Task<string> GenerateOpenQuestionsSectionAsync(string yesterday)
    {
        var yesterdayLog = _memoryStore.GetRecentMemories(2);
        
        if (string.IsNullOrWhiteSpace(yesterdayLog))
        {
            return string.Empty;
        }

        // 查找包含问题标记的行
        var questionPatterns = new[] { "?", "TODO", "todo", "待", "问题", "question", "需要" };
        var questions = yesterdayLog.Split('\n')
            .Where(l => questionPatterns.Any(p => l.Contains(p)))
            .Take(5)
            .ToList();

        if (questions.Count == 0)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### ❌ Unresolved Questions",
            ""
        };

        foreach (var q in questions)
        {
            lines.Add(q.Trim());
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成统计部分（与 v3 模板一致：工具使用 + 启动等）
    /// </summary>
    private string GenerateStatsSection()
    {
        if (_statisticsReporter != null)
            return _statisticsReporter.GetStatsBlockForBriefing(TimeSpan.FromDays(1));

        var analytics = _analyticsService.GetAnalytics();
        var lines = new List<string> { BriefingTemplate.SectionTools, "" };
        var topTools = analytics.GetTopTools(5);
        if (topTools.Count > 0)
            foreach (var t in topTools)
                lines.Add($"- {t.Key}: {t.Value} 次");
        else
            lines.Add("- 无记录");
        lines.Add("");
        lines.Add($"- 启动次数: {analytics.BootCount}");
        lines.Add($"- 平均启动时间: {analytics.AverageBootMs}ms");
        lines.Add("");
        return string.Join("\n", lines);
    }

    private string GenerateActiveFilesSection()
    {
        return ""; // 可后续从 Git 或记忆提取活跃文件
    }

    private async Task<string> GenerateTodoSectionAsync()
    {
        var recent = _memoryStore.GetRecentMemories(2);
        var todoPatterns = new[] { "TODO", "todo", "待办", "[ ]", "未完成" };
        var items = recent.Split('\n')
            .Where(l => todoPatterns.Any(p => l.Contains(p)))
            .Select(l => l.Trim().Length > 80 ? l.Trim()[..77] + "..." : l.Trim())
            .Take(5)
            .ToArray();
        return BriefingTemplate.FormatTodoSection(items);
    }

    private string GenerateSuggestionsSection()
    {
        var analytics = _analyticsService.GetAnalytics();
        var suggestions = new List<string>();
        var top = analytics.GetTopTools(1).FirstOrDefault();
        if (!string.IsNullOrEmpty(top.Key))
            suggestions.Add($"继续使用常用工具「{top.Key}」完成相关任务");
        if (analytics.SkillUsage.Count > 0)
            suggestions.Add("尝试已配置的 Skill 提升效率");
        return BriefingTemplate.FormatSuggestionsSection(suggestions.ToArray());
    }

    /// <summary>
    /// 生成实体摘要部分
    /// </summary>
    private async Task<string> GenerateEntitySectionAsync()
    {
        if (_entityStore == null) return string.Empty;

        var entities = await _entityStore.ListAsync();
        if (entities.Count == 0) return string.Empty;

        var lines = new List<string>
        {
            BriefingTemplate.SectionMemory,
            $"（{entities.Count} 个实体）",
            ""
        };

        var recentEntities = entities
            .OrderByDescending(e => e.LastMentioned)
            .Take(5)
            .ToList();

        foreach (var e in recentEntities)
        {
            lines.Add($"- **{e.Name}** ({e.Type}, {e.MentionCount}x) — last: {e.LastMentioned}");
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成健康检查部分
    /// </summary>
    private string GenerateHealthSection()
    {
        // 检查是否需要蒸馏
        var recent = _memoryStore.GetRecentMemories(1);
        var entryCount = recent.Split('\n').Count(l => l.TrimStart().StartsWith("- ["));

        if (entryCount < 10)
        {
            return string.Empty;
        }

        var lines = new List<string>
        {
            "### 🏥 Health",
            ""
        };

        if (entryCount > 20)
        {
            lines.Add($"⚠️ Memory has {entryCount} entries. Consider distilling.");
        }
        else
        {
            lines.Add($"ℹ️ Memory has {entryCount} entries.");
        }
        lines.Add("");

        return string.Join("\n", lines);
    }

    /// <summary>
    /// 生成简单的单行摘要
    /// </summary>
    public string GenerateOneLineSummary()
    {
        var analytics = _analyticsService.GetAnalytics();
        var parts = new List<string>();

        if (analytics.BootCount > 0)
        {
            parts.Add($"🔄 {analytics.BootCount} boots");
        }

        if (analytics.TotalToolCalls > 0)
        {
            parts.Add($"🔧 {analytics.TotalToolCalls} tool calls");
        }

        var topTool = analytics.GetTopTools(1).FirstOrDefault();
        if (!string.IsNullOrEmpty(topTool.Key))
        {
            parts.Add($"⭐ Top: {topTool.Key}");
        }

        return parts.Count > 0 ? string.Join(" | ", parts) : "No activity yet";
    }
}
