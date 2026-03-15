using MyClaw.Core.Analytics;
using MyClaw.Core.Briefing;
using MyClaw.Core.Entities;
using MyClaw.Core.Memory;

namespace MyClaw.Core.Tests.Briefing;

public class DailyBriefingServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly MemoryStore _memoryStore;
    private readonly AnalyticsService _analyticsService;
    private readonly EntityStore _entityStore;
    private readonly DailyBriefingService _service;

    public DailyBriefingServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"briefing_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
        
        _memoryStore = new MemoryStore(_tempDir);
        _analyticsService = new AnalyticsService(_tempDir);
        _entityStore = new EntityStore(_tempDir);
        _service = new DailyBriefingService(_memoryStore, _analyticsService, _entityStore);
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
    public async Task GenerateBriefingAsync_IncludesHeader()
    {
        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("Daily Briefing", briefing);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), briefing);
    }

    [Fact]
    public async Task GenerateBriefingAsync_WithMemory_IncludesActivity()
    {
        // 添加一些记忆
        _memoryStore.AppendToday("- [10:00] Started working on feature A");
        _memoryStore.AppendToday("- [11:30] Meeting with team");
        _memoryStore.AppendToday("- [14:00] Implemented feature B");

        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("Yesterday's Activity", briefing);
        Assert.Contains("Started working on feature A", briefing);
    }

    [Fact]
    public async Task GenerateBriefingAsync_WithQuestions_IncludesOpenQuestions()
    {
        _memoryStore.AppendToday("- [10:00] How to handle error cases?");
        _memoryStore.AppendToday("- [11:00] TODO: add unit tests");

        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("Unresolved Questions", briefing);
    }

    [Fact]
    public async Task GenerateBriefingAsync_IncludesStats()
    {
        _analyticsService.TrackBoot(100);
        _analyticsService.TrackToolCall("git_status");

        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("工具使用", briefing);
        Assert.Contains("启动次数", briefing);
        Assert.Contains("git_status", briefing);
    }

    [Fact]
    public async Task GenerateBriefingAsync_WithEntities_IncludesEntities()
    {
        await _entityStore.AddAsync(new Entity
        {
            Name = "React",
            Type = EntityType.Tool,
            MentionCount = 5
        });

        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("新增记忆", briefing);
        Assert.Contains("React", briefing);
    }

    [Fact]
    public void GenerateOneLineSummary_WithActivity_ReturnsSummary()
    {
        _analyticsService.TrackBoot(100);
        _analyticsService.TrackToolCall("tool1");
        _analyticsService.TrackToolCall("tool1");

        var summary = _service.GenerateOneLineSummary();

        Assert.Contains("1 boots", summary);
        Assert.Contains("2 tool calls", summary);
        Assert.Contains("tool1", summary);
    }

    [Fact]
    public void GenerateOneLineSummary_NoActivity_ReturnsDefault()
    {
        var summary = _service.GenerateOneLineSummary();

        Assert.Equal("No activity yet", summary);
    }

    [Fact]
    public async Task GenerateBriefingAsync_WithHighMemory_IncludesHealthWarning()
    {
        // 添加大量条目触发健康警告
        for (int i = 0; i < 25; i++)
        {
            _memoryStore.AppendToday($"- [10:{i:D2}] Entry {i}");
        }

        var briefing = await _service.GenerateBriefingAsync();

        Assert.Contains("Health", briefing);
        Assert.Contains("entries", briefing);
    }
}
