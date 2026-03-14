using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace MyClaw.Integration.Tests.Mcp;

/// <summary>
/// RIBOSOME 集成测试 - 验证核糖体加载器与 MCP 服务的集成
/// </summary>
public class RibosomeIntegrationTests : IClassFixture<McpTestFixture>, IAsyncLifetime
{
    private readonly McpTestFixture _fixture;
    private readonly HttpClient _client;

    public RibosomeIntegrationTests(McpTestFixture fixture)
    {
        _fixture = fixture;
        _client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()
    {
        _client.Dispose();
        return Task.CompletedTask;
    }

    #region Tools List from RIBOSOME

    [Fact]
    public async Task ToolsList_ShouldIncludeRibosomeTools()
    {
        var response = await SendJsonRpcAsync("tools/list", new { });

        Assert.NotNull(response.Result);
        var tools = response.Result.Value.GetProperty("tools").EnumerateArray().ToList();
        var toolNames = tools.Select(t => t.GetProperty("name").GetString()).ToList();

        // 验证 RIBOSOME 中定义的核心工具
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
        var response = await SendJsonRpcAsync("tools/list", new { });

        var tools = response.Result!.Value.GetProperty("tools").EnumerateArray();
        var updateTool = tools.First(t => t.GetProperty("name").GetString() == "myclaw_update");

        var description = updateTool.GetProperty("description").GetString();
        Assert.Contains("神经重塑", description);
    }

    [Fact]
    public async Task ToolsList_ShouldHaveValidInputSchemas()
    {
        var response = await SendJsonRpcAsync("tools/list", new { });

        var tools = response.Result!.Value.GetProperty("tools").EnumerateArray();
        var noteTool = tools.First(t => t.GetProperty("name").GetString() == "myclaw_note");

        var schema = noteTool.GetProperty("inputSchema");
        Assert.Equal("object", schema.GetProperty("type").GetString());
        Assert.True(schema.TryGetProperty("properties", out _));
        Assert.True(schema.TryGetProperty("required", out _));
    }

    #endregion

    #region myclaw_skill Tests

    [Fact]
    public async Task Skill_List_ShouldReturnSkills()
    {
        var response = await CallToolAsync("myclaw_skill", new { action = "list" });
        var text = GetToolResultText(response);

        // 可能返回 "没有已安装的技能" 或 "Skills (n)"
        Assert.True(text.Contains("Skills") || text.Contains("没有已安装的技能"));
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
        Assert.Contains("已创建", text);

        // 验证文件已创建
        var skillPath = Path.Combine(_fixture.WorkspacePath, "skills", "test_skill.md");
        Assert.True(File.Exists(skillPath));
    }

    [Fact]
    public async Task Skill_Delete_ShouldRemoveSkill()
    {
        // 先创建
        await CallToolAsync("myclaw_skill", new
        {
            action = "create",
            name = "skill_to_delete",
            description = "To be deleted",
            content = "Content"
        });

        // 再删除
        var response = await CallToolAsync("myclaw_skill", new
        {
            action = "delete",
            name = "skill_to_delete"
        });

        var text = GetToolResultText(response);
        Assert.Contains("已删除", text);
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

        // 没有日志时返回没有今日日志
        Assert.True(text.Contains("没有今日日志") || text.Contains("Dream Analysis"));
    }

    [Fact]
    public async Task Dream_WithLog_ShouldReturnAnalysis()
    {
        // 先记录一些日志
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
        // 先创建一些核心文件
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Soul content" });

        var response = await CallToolAsync("myclaw_immune", new { });
        var text = GetToolResultText(response);

        Assert.Contains("免疫升级", text);
        Assert.Contains("已备份", text);

        // 验证备份目录存在
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

        Assert.Contains("没有找到备份", text);
    }

    [Fact]
    public async Task Heal_WithBackup_ShouldRestore()
    {
        // 创建备份
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "Original user content" });
        await CallToolAsync("myclaw_immune", new { });

        // 修改文件
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "Modified content" });

        // 恢复
        var response = await CallToolAsync("myclaw_heal", new { });
        var text = GetToolResultText(response);

        Assert.Contains("基因修复", text);
        Assert.Contains("已恢复", text);
    }

    #endregion

    #region myclaw_nociception Tests

    [Fact]
    public async Task Nociception_List_ShouldReturnEmptyOrList()
    {
        var response = await CallToolAsync("myclaw_nociception", new { action = "list" });
        var text = GetToolResultText(response);

        // 可能返回空或已有记录
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
        Assert.Contains("痛觉记忆已记录", text);

        // 验证文件已创建
        var nociceptionPath = Path.Combine(_fixture.WorkspacePath, "NOCICEPTION.md");
        Assert.True(File.Exists(nociceptionPath));
    }

    [Fact]
    public async Task Nociception_Check_ShouldReturnWarningOrSafe()
    {
        // 先记录一个
        await CallToolAsync("myclaw_nociception", new
        {
            action = "record",
            stimulus = "rm_rf",
            harm = "deleted_everything",
            strategy = "never_use_rm_rf"
        });

        // 检查匹配的
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
        // 先记录
        await CallToolAsync("myclaw_nociception", new
        {
            action = "record",
            stimulus = "temp",
            harm = "test",
            strategy = "test"
        });

        // 清除
        var response = await CallToolAsync("myclaw_nociception", new { action = "clear" });
        var text = GetToolResultText(response);

        Assert.Contains("痛觉记忆已清除", text);
    }

    #endregion

    #region Helper Methods

    private async Task<JsonRpcResponse> SendJsonRpcAsync(string method, object? Params)
    {
        var request = new
        {
            jsonrpc = "2.0",
            id = Guid.NewGuid().ToString(),
            method,
            @params = Params
        };

        var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
        var httpResponse = await _client.PostAsync($"http://localhost:{_fixture.Port}/mcp", content);
        var body = await httpResponse.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);

        return new JsonRpcResponse
        {
            JsonRpc = doc.RootElement.GetProperty("jsonrpc").GetString()!,
            Id = doc.RootElement.TryGetProperty("id", out var idEl) ? idEl.GetString() : null,
            Result = doc.RootElement.TryGetProperty("result", out var resultEl) ? resultEl : null,
            Error = doc.RootElement.TryGetProperty("error", out var errorEl) ? errorEl : null
        };
    }

    private async Task<JsonRpcResponse> CallToolAsync(string toolName, object arguments)
    {
        return await SendJsonRpcAsync("tools/call", new
        {
            name = toolName,
            arguments
        });
    }

    private static string GetToolResultText(JsonRpcResponse response)
    {
        if (response.Result == null) return string.Empty;

        var content = response.Result.Value.GetProperty("content").EnumerateArray().First();
        return content.GetProperty("text").GetString() ?? string.Empty;
    }

    private class JsonRpcResponse
    {
        public string JsonRpc { get; set; } = string.Empty;
        public string? Id { get; set; }
        public JsonElement? Result { get; set; }
        public JsonElement? Error { get; set; }
    }

    #endregion
}
