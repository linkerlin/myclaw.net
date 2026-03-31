namespace MyClaw.Core.Ace;

/// <summary>
/// 时间模式类型
/// </summary>
public enum TimeMode
{
    Morning,    // 早晨 06-09
    Work,       // 工作 09-12, 14-18
    Break,      // 休息 12-14
    Evening,    // 晚上 18-22
    Night       // 深夜 22-06
}

/// <summary>
/// 时间模式配置
/// </summary>
public class TimeModeConfig
{
    /// <summary>
    /// Emoji 图标
    /// </summary>
    public string Emoji { get; set; } = string.Empty;

    /// <summary>
    /// 标签
    /// </summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>
    /// 是否显示简报
    /// </summary>
    public bool ShowBriefing { get; set; }

    /// <summary>
    /// 是否建议反思
    /// </summary>
    public bool SuggestReflective { get; set; }

    /// <summary>
    /// 是否极简模式
    /// </summary>
    public bool MinimalMode { get; set; }
}

/// <summary>
/// 时间模式管理器
/// </summary>
public static class TimeModeManager
{
    private static readonly Dictionary<TimeMode, TimeModeConfig> Configs = new()
    {
        [TimeMode.Morning] = new TimeModeConfig
        {
            Emoji = "☀️",
            Label = "Morning",
            ShowBriefing = true,
            SuggestReflective = false,
            MinimalMode = false
        },
        [TimeMode.Work] = new TimeModeConfig
        {
            Emoji = "💼",
            Label = "Work",
            ShowBriefing = false,
            SuggestReflective = false,
            MinimalMode = false
        },
        [TimeMode.Break] = new TimeModeConfig
        {
            Emoji = "🍜",
            Label = "Break",
            ShowBriefing = false,
            SuggestReflective = false,
            MinimalMode = false
        },
        [TimeMode.Evening] = new TimeModeConfig
        {
            Emoji = "🌙",
            Label = "Evening",
            ShowBriefing = false,
            SuggestReflective = true,
            MinimalMode = false
        },
        [TimeMode.Night] = new TimeModeConfig
        {
            Emoji = "😴",
            Label = "Night",
            ShowBriefing = false,
            SuggestReflective = false,
            MinimalMode = true
        }
    };

    /// <summary>
    /// 获取当前时间模式
    /// </summary>
    public static TimeMode GetCurrentMode()
    {
        return GetCurrentMode(DateTime.Now);
    }

    /// <summary>
    /// 根据指定时间获取时间模式
    /// </summary>
    public static TimeMode GetCurrentMode(DateTime now)
    {
        var hour = now.Hour;

        return hour switch
        {
            >= 6 and < 9 => TimeMode.Morning,
            >= 9 and < 12 => TimeMode.Work,
            >= 12 and < 14 => TimeMode.Break,
            >= 14 and < 18 => TimeMode.Work,
            >= 18 and < 22 => TimeMode.Evening,
            _ => TimeMode.Night
        };
    }

    /// <summary>
    /// 获取时间模式配置
    /// </summary>
    public static TimeModeConfig GetConfig(TimeMode mode)
    {
        return Configs[mode];
    }
}
