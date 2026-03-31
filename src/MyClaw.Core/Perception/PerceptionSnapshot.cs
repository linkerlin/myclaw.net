namespace MyClaw.Core.Perception;

/// <summary>
/// 平台感知快照
/// </summary>
public class PerceptionSnapshot
{
    /// <summary>
    /// ACE 上下文优先级提示
    /// </summary>
    public int PriorityHint { get; set; } = 6;

    /// <summary>
    /// 平台名称
    /// </summary>
    public string Platform { get; set; } = string.Empty;

    /// <summary>
    /// Provider 名称
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// 专注模式 / DND 状态
    /// </summary>
    public string FocusMode { get; set; } = "unavailable";

    /// <summary>
    /// 电池状态
    /// </summary>
    public string Battery { get; set; } = "unavailable";

    /// <summary>
    /// 活跃应用列表
    /// </summary>
    public List<string> ActiveApplications { get; set; } = new();

    /// <summary>
    /// 额外说明
    /// </summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>
    /// 捕获时间
    /// </summary>
    public DateTime CapturedAt { get; set; }

    public string ToContextString()
    {
        var lines = new List<string>
        {
            "## PERCEPTION",
            $"- Platform: {Platform}",
            $"- Provider: {Provider}",
            $"- Focus mode: {FocusMode}",
            $"- Battery: {Battery}"
        };

        if (ActiveApplications.Count > 0)
        {
            lines.Add($"- Active apps: {string.Join(", ", ActiveApplications.Take(5))}");
        }

        foreach (var note in Notes.Where(note => !string.IsNullOrWhiteSpace(note)).Take(3))
        {
            lines.Add($"- Note: {note}");
        }

        return string.Join("\n", lines);
    }
}