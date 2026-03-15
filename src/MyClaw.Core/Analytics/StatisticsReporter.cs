namespace MyClaw.Core.Analytics;

/// <summary>
/// 统计报告生成 - 汇总工具使用、启动、简报等（与 MiniClaw 每日简报对齐）
/// </summary>
public class StatisticsReporter
{
    private readonly AnalyticsService _analyticsService;
    private readonly ToolUsageTracker? _toolTracker;

    public StatisticsReporter(AnalyticsService analyticsService, ToolUsageTracker? toolTracker = null)
    {
        _analyticsService = analyticsService;
        _toolTracker = toolTracker;
    }

    /// <summary>
    /// 获取昨日工具使用摘要（用于简报）
    /// </summary>
    public string GetToolUsageSummaryForBriefing(TimeSpan period)
    {
        if (_toolTracker == null)
        {
            var analytics = _analyticsService.GetAnalytics();
            var top = analytics.GetTopTools(5);
            if (top.Count == 0) return "- 无记录";
            return string.Join("\n", top.Select(t => $"- {t.Key}: {t.Value} 次"));
        }

        var stats = _toolTracker.GetStats(period);
        if (stats.TotalCalls == 0) return "- 无记录";
        return string.Join("\n", stats.MostUsedTools.Select(t => $"- {t.Key}: {t.Value} 次"));
    }

    /// <summary>
    /// 获取简报用统计块（工具 + 启动 + 技能）
    /// </summary>
    public string GetStatsBlockForBriefing(TimeSpan toolPeriod)
    {
        var lines = new List<string> { "### 🛠️ 工具使用", "" };
        lines.Add(GetToolUsageSummaryForBriefing(toolPeriod));
        lines.Add("");

        var analytics = _analyticsService.GetAnalytics();
        lines.Add($"- 启动次数: {analytics.BootCount}");
        lines.Add($"- 平均启动时间: {analytics.AverageBootMs}ms");
        if (analytics.TotalSkillUsage > 0)
            lines.Add($"- 技能调用: {analytics.TotalSkillUsage} 次");
        lines.Add("");
        return string.Join("\n", lines);
    }
}
