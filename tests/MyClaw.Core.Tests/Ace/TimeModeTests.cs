using MyClaw.Core.Ace;

namespace MyClaw.Core.Tests.Ace;

public class TimeModeTests
{
    [Theory]
    [InlineData(6, TimeMode.Morning)]
    [InlineData(8, TimeMode.Morning)]
    [InlineData(9, TimeMode.Work)]
    [InlineData(13, TimeMode.Break)]
    [InlineData(15, TimeMode.Work)]
    [InlineData(19, TimeMode.Evening)]
    [InlineData(23, TimeMode.Night)]
    [InlineData(3, TimeMode.Night)]
    public void GetCurrentMode_WithSpecificTime_ShouldReturnExpectedMode(int hour, TimeMode expectedMode)
    {
        var now = new DateTime(2026, 1, 1, hour, 0, 0);

        var mode = TimeModeManager.GetCurrentMode(now);

        Assert.Equal(expectedMode, mode);
    }

    [Theory]
    [InlineData(TimeMode.Morning)]
    [InlineData(TimeMode.Work)]
    [InlineData(TimeMode.Break)]
    [InlineData(TimeMode.Evening)]
    [InlineData(TimeMode.Night)]
    public void GetCurrentMode_ShouldReturnCorrectMode(TimeMode expectedMode)
    {
        // 注意：这个测试依赖于当前时间，实际测试可能需要 mock
        // 这里我们只是测试 GetConfig 的逻辑
        var config = TimeModeManager.GetConfig(expectedMode);

        Assert.NotNull(config);
        Assert.NotEmpty(config.Label);
        Assert.NotEmpty(config.Emoji);
    }

    [Theory]
    [InlineData(TimeMode.Morning, "☀️", "Morning", true, false, false)]
    [InlineData(TimeMode.Work, "💼", "Work", false, false, false)]
    [InlineData(TimeMode.Break, "🍜", "Break", false, false, false)]
    [InlineData(TimeMode.Evening, "🌙", "Evening", false, true, false)]
    [InlineData(TimeMode.Night, "😴", "Night", false, false, true)]
    public void GetConfig_ShouldReturnCorrectConfiguration(
        TimeMode mode, 
        string expectedEmoji, 
        string expectedLabel,
        bool expectedBriefing,
        bool expectedReflective,
        bool expectedMinimal)
    {
        var config = TimeModeManager.GetConfig(mode);

        Assert.Equal(expectedEmoji, config.Emoji);
        Assert.Equal(expectedLabel, config.Label);
        Assert.Equal(expectedBriefing, config.ShowBriefing);
        Assert.Equal(expectedReflective, config.SuggestReflective);
        Assert.Equal(expectedMinimal, config.MinimalMode);
    }

    [Fact]
    public void GetConfig_AllModes_ShouldHaveValidConfiguration()
    {
        foreach (TimeMode mode in Enum.GetValues(typeof(TimeMode)))
        {
            var config = TimeModeManager.GetConfig(mode);

            Assert.NotNull(config);
            Assert.False(string.IsNullOrEmpty(config.Emoji));
            Assert.False(string.IsNullOrEmpty(config.Label));
        }
    }
}
