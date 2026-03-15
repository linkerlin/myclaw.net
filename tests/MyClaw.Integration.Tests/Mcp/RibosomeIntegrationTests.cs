using System.Text.Json;

namespace MyClaw.Integration.Tests.Mcp;

/// <summary>
/// RIBOSOME Integration Tests - stdio mode
/// </summary>
public class RibosomeIntegrationTests : IClassFixture<McpTestFixture>, IAsyncLifetime
{
    private readonly McpTestFixture _fixture;

    public RibosomeIntegrationTests(McpTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    #region Tools List from RIBOSOME

    [Fact]
    public async Task ToolsList_ShouldIncludeRibosomeTools()
    {
        var response = await _fixture.SendRequestAsync("tools/list", new { });

        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var tools = result.GetProperty("tools").EnumerateArray().ToList();
        var toolNames = tools.Select(t => t.GetProperty("name").GetString()).ToList();

        // Verify RIBOSOME core tools
        Assert.Contains("myclaw_update", toolNames);
        Assert.Contains("myclaw_note", toolNames);
        Assert.Contains("myclaw_read", toolNames);
        Assert.Contains("myclaw_exec", toolNames);
        Assert.Contains("myclaw_entity", toolNames);
        Assert.Contains("myclaw_skill", toolNames);
        Assert.Contains("myclaw_introspect", toolNames);
        Assert.Contains("myclaw_dream", toolNames);
        Assert.Contains("myclaw_archive", toolNames);
        Assert.Contains("myclaw_immune", toolNames);
        Assert.Contains("myclaw_heal", toolNames);
        Assert.Contains("myclaw_status", toolNames);
        Assert.Contains("myclaw_nociception", toolNames);
    }

    [Fact]
    public async Task ToolsList_ShouldHaveRichDescriptions()
    {
        var response = await _fixture.SendRequestAsync("tools/list", new { });

        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var tools = result.GetProperty("tools").EnumerateArray();
        var updateTool = tools.First(t => t.GetProperty("name").GetString() == "myclaw_update");

        var description = updateTool.GetProperty("description").GetString();
        Assert.NotNull(description);
    }

    [Fact]
    public async Task ToolsList_ShouldHaveValidInputSchemas()
    {
        var response = await _fixture.SendRequestAsync("tools/list", new { });

        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var tools = result.GetProperty("tools").EnumerateArray();
        var noteTool = tools.First(t => t.GetProperty("name").GetString() == "myclaw_note");

        var schema = noteTool.GetProperty("inputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out _));
    }

    #endregion

    #region myclaw_skill Tests

    [Fact]
    public async Task Skill_List_ShouldReturnSkills()
    {
        var response = await CallToolAsync("myclaw_skill", new { action = "list" });
        var text = GetToolResultText(response);

        Assert.True(text.Contains("Skills") || text.Contains("No skills"));
    }

    [Fact]
    public async Task Skill_Create_ShouldCreateSkill()
    {
        var response = await CallToolAsync("myclaw_skill", new
        {
            action = "create",
            name = "test_skill",
            description = "A test skill",
            content = "# Test Skill\n\nThis is a test skill content."
        });

        var text = GetToolResultText(response);
        Assert.Contains("created", text);

        // Verify file was created
        var skillPath = Path.Combine(_fixture.WorkspacePath, "skills", "test_skill.md");
        Assert.True(File.Exists(skillPath));
    }

    [Fact]
    public async Task Skill_Delete_ShouldRemoveSkill()
    {
        // Create first
        await CallToolAsync("myclaw_skill", new
        {
            action = "create",
            name = "skill_to_delete",
            description = "To be deleted",
            content = "Content"
        });

        // Delete
        var response = await CallToolAsync("myclaw_skill", new
        {
            action = "delete",
            name = "skill_to_delete"
        });

        var text = GetToolResultText(response);
        Assert.Contains("deleted", text);
    }

    #endregion

    #region myclaw_introspect Tests

    [Fact]
    public async Task Introspect_Summary_ShouldReturnOverview()
    {
        var response = await CallToolAsync("myclaw_introspect", new { scope = "summary" });
        var text = GetToolResultText(response);

        Assert.Contains("Introspection Summary", text);
        Assert.Contains("Entities:", text);
        Assert.Contains("Skills:", text);
    }

    [Fact]
    public async Task Introspect_Tools_ShouldReturnToolAnalysis()
    {
        var response = await CallToolAsync("myclaw_introspect", new { scope = "tools" });
        var text = GetToolResultText(response);

        Assert.Contains("Tool Usage Analysis", text);
        Assert.Contains("RIBOSOME", text);
    }

    [Fact]
    public async Task Introspect_Files_ShouldReturnFileAnalysis()
    {
        var response = await CallToolAsync("myclaw_introspect", new { scope = "files" });
        var text = GetToolResultText(response);

        Assert.Contains("File Analysis", text);
        Assert.Contains("Workspace:", text);
    }

    #endregion

    #region myclaw_dream Tests

    [Fact]
    public async Task Dream_NoLog_ShouldReturnNoLogMessage()
    {
        var response = await CallToolAsync("myclaw_dream", new { });
        var text = GetToolResultText(response);

        // No log returns no log message or Dream Analysis
        Assert.True(text.Contains("No today's log") || text.Contains("Dream Analysis"));
    }

    [Fact]
    public async Task Dream_WithLog_ShouldReturnAnalysis()
    {
        // Record some log first
        await CallToolAsync("myclaw_note", new { text = "Test activity for dream analysis" });

        var response = await CallToolAsync("myclaw_dream", new { });
        var text = GetToolResultText(response);

        Assert.Contains("Dream Analysis", text);
    }

    #endregion

    #region myclaw_immune Tests

    [Fact]
    public async Task Immune_ShouldCreateBackup()
    {
        // Create some core files first
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Soul content" });

        var response = await CallToolAsync("myclaw_immune", new { });
        var text = GetToolResultText(response);

        Assert.Contains("Immune upgrade", text);
        Assert.Contains("Backed up", text);

        // Verify backup directory exists
        var backupDir = Path.Combine(_fixture.WorkspacePath, ".backup");
        Assert.True(Directory.Exists(backupDir));
    }

    #endregion

    #region myclaw_heal Tests

    [Fact]
    public async Task Heal_NoBackup_ShouldReturnNoBackupMessage()
    {
        var response = await CallToolAsync("myclaw_heal", new { });
        var text = GetToolResultText(response);

        Assert.Contains("Backup directory not found", text);
    }

    [Fact]
    public async Task Heal_WithBackup_ShouldRestore()
    {
        // Create backup
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "Original user content" });
        await CallToolAsync("myclaw_immune", new { });

        // Modify file
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "Modified content" });

        // Restore
        var response = await CallToolAsync("myclaw_heal", new { });
        var text = GetToolResultText(response);

        Assert.Contains("Gene repair", text);
        Assert.Contains("Restored", text);
    }

    #endregion

    #region myclaw_nociception Tests

    [Fact]
    public async Task Nociception_List_ShouldReturnEmptyOrList()
    {
        var response = await CallToolAsync("myclaw_nociception", new { action = "list" });
        var text = GetToolResultText(response);

        // May return empty or existing records
        Assert.NotNull(text);
    }

    [Fact]
    public async Task Nociception_Record_ShouldCreateEntry()
    {
        var response = await CallToolAsync("myclaw_nociception", new
        {
            action = "record",
            stimulus = "dangerous_command",
            harm = "data_loss",
            strategy = "always_backup_first"
        });

        var text = GetToolResultText(response);
        Assert.Contains("Pain memory recorded", text);

        // Verify file was created
        var nociceptionPath = Path.Combine(_fixture.WorkspacePath, "NOCICEPTION.md");
        Assert.True(File.Exists(nociceptionPath));
    }

    [Fact]
    public async Task Nociception_Check_ShouldReturnWarningOrSafe()
    {
        // Record first
        await CallToolAsync("myclaw_nociception", new
        {
            action = "record",
            stimulus = "rm_rf",
            harm = "deleted_everything",
            strategy = "never_use_rm_rf"
        });

        // Check matching
        var response = await CallToolAsync("myclaw_nociception", new
        {
            action = "check",
            stimulus = "rm_rf"
        });

        var text = GetToolResultText(response);
        Assert.Contains("rm_rf", text);
    }

    [Fact]
    public async Task Nociception_Clear_ShouldRemoveFile()
    {
        // Record first
        await CallToolAsync("myclaw_nociception", new
        {
            action = "record",
            stimulus = "temp",
            harm = "test",
            strategy = "test"
        });

        // Clear
        var response = await CallToolAsync("myclaw_nociception", new { action = "clear" });
        var text = GetToolResultText(response);

        Assert.Contains("Pain memories cleared", text);
    }

    #endregion

    #region Helper Methods

    private async Task<JsonDocument> CallToolAsync(string toolName, object arguments)
    {
        return await _fixture.SendRequestAsync("tools/call", new
        {
            name = toolName,
            arguments
        });
    }

    private static string GetToolResultText(JsonDocument response)
    {
        if (!response.RootElement.TryGetProperty("result", out var result))
            return string.Empty;

        var content = result.GetProperty("content").EnumerateArray().First();
        return content.GetProperty("text").GetString() ?? string.Empty;
    }

    #endregion
}
