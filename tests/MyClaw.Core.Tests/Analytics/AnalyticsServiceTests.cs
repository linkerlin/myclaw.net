using MyClaw.Core.Analytics;

namespace MyClaw.Core.Tests.Analytics;

public class AnalyticsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly AnalyticsService _service;

    public AnalyticsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"analytics_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        _service = new AnalyticsService(_tempDir);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
                Directory.Delete(_tempDir, true);
        }
        catch { }
    }

    [Fact]
    public void TrackToolCall_NewTool_IncrementsCount()
    {
        _service.TrackToolCall("git_status");
        _service.TrackToolCall("git_status");
        _service.TrackToolCall("git_status");

        var analytics = _service.GetAnalytics();

        Assert.Equal(3, analytics.ToolCalls["git_status"]);
    }

    [Fact]
    public void TrackToolCall_MultipleTools_TracksSeparately()
    {
        _service.TrackToolCall("tool_a");
        _service.TrackToolCall("tool_b");
        _service.TrackToolCall("tool_a");

        var analytics = _service.GetAnalytics();

        Assert.Equal(2, analytics.ToolCalls["tool_a"]);
        Assert.Equal(1, analytics.ToolCalls["tool_b"]);
    }

    [Fact]
    public void TrackPrompt_TracksPromptUsage()
    {
        _service.TrackPrompt("coding");
        _service.TrackPrompt("coding");
        _service.TrackPrompt("writing");

        var analytics = _service.GetAnalytics();

        Assert.Equal(2, analytics.PromptsUsed["coding"]);
        Assert.Equal(1, analytics.PromptsUsed["writing"]);
    }

    [Fact]
    public void TrackSkillUsage_TracksSkillUsage()
    {
        _service.TrackSkillUsage("lua-writing");
        _service.TrackSkillUsage("lua-writing");

        var analytics = _service.GetAnalytics();

        Assert.Equal(2, analytics.SkillUsage["lua-writing"]);
    }

    [Fact]
    public void TrackBoot_TracksBootStats()
    {
        _service.TrackBoot(100);
        _service.TrackBoot(200);
        _service.TrackBoot(300);

        var analytics = _service.GetAnalytics();

        Assert.Equal(3, analytics.BootCount);
        Assert.Equal(600, analytics.TotalBootMs);
        Assert.Equal(200, analytics.AverageBootMs);
    }

    [Fact]
    public void TrackDistillation_IncrementsDistillationCount()
    {
        _service.TrackDistillation();
        _service.TrackDistillation();

        var analytics = _service.GetAnalytics();

        Assert.Equal(2, analytics.DailyDistillations);
    }

    [Fact]
    public void TrackBoredomExecution_SetsTimestampAndPersists()
    {
        var executedAt = new DateTime(2026, 3, 31, 10, 0, 0, DateTimeKind.Utc);

        _service.TrackBoredomExecution(executedAt);

        var analytics = _service.GetAnalytics();

        Assert.Equal(executedAt.ToString("O"), analytics.LastBoredomExecution);

        var reloaded = new AnalyticsService(_tempDir).GetAnalytics();
        Assert.Equal(executedAt.ToString("O"), reloaded.LastBoredomExecution);
    }

    [Fact]
    public void GetTopTools_ReturnsTopToolsOrdered()
    {
        _service.TrackToolCall("tool_a"); // 1
        _service.TrackToolCall("tool_b"); // 1
        _service.TrackToolCall("tool_b"); // 2
        _service.TrackToolCall("tool_c"); // 1
        _service.TrackToolCall("tool_c"); // 2
        _service.TrackToolCall("tool_c"); // 3

        var topTools = _service.GetAnalytics().GetTopTools(2);

        Assert.Equal(2, topTools.Count);
        Assert.Equal("tool_c", topTools[0].Key);
        Assert.Equal(3, topTools[0].Value);
        Assert.Equal("tool_b", topTools[1].Key);
    }

    [Fact]
    public void TotalToolCalls_ReturnsSum()
    {
        _service.TrackToolCall("a");
        _service.TrackToolCall("b");
        _service.TrackToolCall("b");

        var total = _service.GetAnalytics().TotalToolCalls;

        Assert.Equal(3, total);
    }

    [Fact]
    public void DetectChanges_DetectsNewSections()
    {
        var current = new Dictionary<string, string>
        {
            ["section1"] = "hash1",
            ["section2"] = "hash2"
        };

        var (changed, unchanged, newSections) = _service.DetectChanges(current);

        Assert.Contains("section1", newSections);
        Assert.Contains("section2", newSections);
        Assert.Empty(changed);
        Assert.Empty(unchanged);
    }

    [Fact]
    public void DetectChanges_DetectsChangedSections()
    {
        // 先设置初始哈希
        _service.UpdateHash("section1", "hash1");
        _service.UpdateHash("section2", "hash2");

        var current = new Dictionary<string, string>
        {
            ["section1"] = "hash1", // 未变
            ["section2"] = "new_hash" // 变了
        };

        var (changed, unchanged, newSections) = _service.DetectChanges(current);

        Assert.Contains("section2", changed);
        Assert.Contains("section1", unchanged);
        Assert.Empty(newSections);
    }

    [Fact]
    public void ResetDailyStats_ResetsDistillationCount()
    {
        _service.TrackDistillation();
        _service.TrackDistillation();
        _service.ResetDailyStats();

        var analytics = _service.GetAnalytics();

        Assert.Equal(0, analytics.DailyDistillations);
    }

    [Fact]
    public void GetHash_ReturnsStoredHash()
    {
        _service.UpdateHash("section1", "abc123");

        var hash = _service.GetHash("section1");

        Assert.Equal("abc123", hash);
    }

    [Fact]
    public void GetHash_NonExistent_ReturnsNull()
    {
        var hash = _service.GetHash("non_existent");

        Assert.Null(hash);
    }

    [Fact]
    public void Persistence_DataSurvivesRecreation()
    {
        _service.TrackToolCall("test_tool");
        _service.TrackBoot(150);

        // 创建新服务实例
        var newService = new AnalyticsService(_tempDir);
        var analytics = newService.GetAnalytics();

        Assert.Equal(1, analytics.ToolCalls["test_tool"]);
        Assert.Equal(1, analytics.BootCount);
    }
}
