namespace MyClaw.Core.Evolution;

/// <summary>
/// 进化报告生成 - 汇总历史与当前状态（与改进方案 v3 进化历史追踪对齐）
/// </summary>
public class EvolutionReport
{
    private readonly EvolutionHistory _history;
    private readonly EvolutionEngine _engine;

    public EvolutionReport(EvolutionHistory history, EvolutionEngine engine)
    {
        _history = history;
        _engine = engine;
    }

    /// <summary>
    /// 生成可读的进化报告（Markdown）
    /// </summary>
    public async Task<string> GenerateReportAsync(int lastN = 10)
    {
        var state = await _engine.GetStateAsync();
        var recent = _history.GetRecent(lastN);

        var lines = new List<string>
        {
            "## 🧬 Evolution Report",
            "",
            $"**Total generations**: {state.TotalEvolutions}",
            state.LastEvolution.HasValue
                ? $"**Last evolution**: {state.LastEvolution.Value:yyyy-MM-dd HH:mm} UTC"
                : "**Last evolution**: never",
            state.CanEvolve ? "**Status**: Ready to evolve" : $"**Status**: Cooldown ({state.RemainingCooldownHours:F0}h remaining)",
            ""
        };

        if (recent.Count > 0)
        {
            lines.Add("### Recent Evolutions");
            lines.Add("");
            foreach (var e in recent)
            {
                lines.Add($"- **G{e.Generation}** {e.Timestamp:yyyy-MM-dd HH:mm} — {e.MutationCount} mutations ({string.Join(", ", e.PatternTypes)})");
            }
            lines.Add("");
        }

        return string.Join("\n", lines);
    }
}
