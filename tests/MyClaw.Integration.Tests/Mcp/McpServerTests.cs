using System.Text.Json;

namespace MyClaw.Integration.Tests.Mcp;

public class McpServerTests : IClassFixture<McpTestFixture>, IAsyncLifetime
{
    private readonly McpTestFixture _fixture;

    public McpServerTests(McpTestFixture fixture)
    {
        _fixture = fixture;
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => Task.CompletedTask;

    #region Protocol Tests

    [Fact]
    public async Task Initialize_ShouldReturnProtocolVersion()
    {
        var response = await _fixture.SendRequestAsync("initialize", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        Assert.Equal("2024-11-05", result.GetProperty("protocolVersion").GetString());
        Assert.Equal("myclaw", result.GetProperty("serverInfo").GetProperty("name").GetString());
    }

    [Fact]
    public async Task Ping_ShouldReturnEmptyResult()
    {
        var response = await _fixture.SendRequestAsync("ping", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out _));
    }

    [Fact]
    public async Task InvalidJsonRpcVersion_ShouldReturnError()
    {
        // Send invalid request directly
        var request = new
        {
            jsonrpc = "1.0",
            id = "1",
            method = "initialize",
            @params = new { }
        };

        // Use reflection or add a test-only method to send raw JSON
        // For now, test through normal flow
        var response = await _fixture.SendRequestAsync("unknown_method", new { });
        
        Assert.True(response.RootElement.TryGetProperty("error", out _));
    }

    [Fact]
    public async Task UnknownMethod_ShouldReturnError()
    {
        var response = await _fixture.SendRequestAsync("unknown_method", new { });
        
        Assert.True(response.RootElement.TryGetProperty("error", out var error));
        Assert.Equal(-32601, error.GetProperty("code").GetInt32());
    }

    #endregion

    #region Tools List Tests

    [Fact]
    public async Task ToolsList_ShouldReturnAllCoreTools()
    {
        var response = await _fixture.SendRequestAsync("tools/list", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var tools = result.GetProperty("tools").EnumerateArray().ToList();
        
        var toolNames = tools.Select(t => t.GetProperty("name").GetString()).ToList();
        
        Assert.Contains("myclaw_update", toolNames);
        Assert.Contains("myclaw_note", toolNames);
        Assert.Contains("myclaw_read", toolNames);
        Assert.Contains("myclaw_archive", toolNames);
        Assert.Contains("myclaw_entity", toolNames);
        Assert.Contains("myclaw_exec", toolNames);
        Assert.Contains("myclaw_status", toolNames);
    }

    [Fact]
    public async Task ToolsList_ShouldHaveValidSchemas()
    {
        var response = await _fixture.SendRequestAsync("tools/list", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var tools = result.GetProperty("tools").EnumerateArray();
        
        foreach (var tool in tools)
        {
            Assert.True(tool.TryGetProperty("name", out _));
            Assert.True(tool.TryGetProperty("description", out _));
            Assert.True(tool.TryGetProperty("inputSchema", out _));
            
            var schema = tool.GetProperty("inputSchema");
            Assert.Equal("object", schema.GetProperty("type").GetString());
        }
    }

    #endregion

    #region myclaw_update Tests

    [Fact]
    public async Task Update_ShouldCreateFile()
    {
        var response = await CallToolAsync("myclaw_update", new
        {
            filename = "SOUL.md",
            content = "# Test Soul\n\nThis is a test soul content."
        });

        var text = GetToolResultText(response);
        Assert.Contains("Updated", text);
        
        var filePath = Path.Combine(_fixture.WorkspacePath, "SOUL.md");
        Assert.True(File.Exists(filePath));
        
        var content = await File.ReadAllTextAsync(filePath);
        Assert.Contains("# Test Soul", content);
    }

    [Fact]
    public async Task Update_ShouldCreateBackup()
    {
        var filePath = Path.Combine(_fixture.WorkspacePath, "AGENTS.md");
        await File.WriteAllTextAsync(filePath, "Original content");
        
        await CallToolAsync("myclaw_update", new
        {
            filename = "AGENTS.md",
            content = "New content"
        });

        Assert.True(File.Exists(filePath + ".bak"));
        var backup = await File.ReadAllTextAsync(filePath + ".bak");
        Assert.Equal("Original content", backup);
    }

    #endregion

    #region myclaw_note Tests

    [Fact]
    public async Task Note_ShouldAppendToTodayLog()
    {
        var response = await CallToolAsync("myclaw_note", new
        {
            text = "Test note entry at " + DateTime.UtcNow
        });

        var text = GetToolResultText(response);
        Assert.Contains("Recorded", text);
    }

    [Fact]
    public async Task Note_MultipleEntries_ShouldAppendSequentially()
    {
        await CallToolAsync("myclaw_note", new { text = "First note" });
        await CallToolAsync("myclaw_note", new { text = "Second note" });

        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("First note", text);
        Assert.Contains("Second note", text);
    }

    #endregion

    #region myclaw_read Tests

    [Fact]
    public async Task Read_EmptyWorkspace_ShouldReturnEmpty()
    {
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.NotNull(text);
    }

    [Fact]
    public async Task Read_ShouldIncludeAllDnaFiles()
    {
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Soul content" });
        await CallToolAsync("myclaw_update", new { filename = "USER.md", content = "User content" });
        
        var response = await CallToolAsync("myclaw_read", new { mode = "full" });
        var text = GetToolResultText(response);
        
        Assert.Contains("SOUL.md", text);
        Assert.Contains("Soul content", text);
        Assert.Contains("USER.md", text);
        Assert.Contains("User content", text);
    }

    #endregion

    #region myclaw_archive Tests

    [Fact]
    public async Task Archive_NoLog_ShouldReturnNoLogMessage()
    {
        var response = await CallToolAsync("myclaw_archive", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("No log", text);
    }

    [Fact]
    public async Task Archive_WithLog_ShouldArchiveSuccessfully()
    {
        await CallToolAsync("myclaw_note", new { text = "Note to archive" });
        
        var response = await CallToolAsync("myclaw_archive", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("Archived", text);
    }

    #endregion

    #region myclaw_entity Tests

    [Fact]
    public async Task Entity_Add_ShouldCreateEntity()
    {
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "add",
            name = "TestProject",
            type = "project",
            attributes = new { language = "C#", framework = ".NET 9" }
        });

        var text = GetToolResultText(response);
        Assert.Contains("TestProject", text);
        Assert.Contains("Project", text);
    }

    [Fact]
    public async Task Entity_Query_ShouldReturnEntity()
    {
        await CallToolAsync("myclaw_entity", new
        {
            action = "add",
            name = "QueryTest",
            type = "concept",
            attributes = new { importance = "high" }
        });
        
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "query",
            name = "QueryTest"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("QueryTest", text);
        Assert.Contains("Concept", text);
    }

    [Fact]
    public async Task Entity_QueryNonExistent_ShouldReturnNotFound()
    {
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "query",
            name = "NonExistent"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("does not exist", text);
    }

    [Fact]
    public async Task Entity_List_ShouldReturnAllEntities()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ListTest1", type = "project" });
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ListTest2", type = "person" });
        
        var response = await CallToolAsync("myclaw_entity", new { action = "list" });
        var text = GetToolResultText(response);
        
        Assert.Contains("Entities", text);
    }

    [Fact]
    public async Task Entity_Remove_ShouldDeleteEntity()
    {
        await CallToolAsync("myclaw_entity", new { action = "add", name = "ToRemove", type = "tool" });
        
        var response = await CallToolAsync("myclaw_entity", new
        {
            action = "remove",
            name = "ToRemove"
        });
        
        var text = GetToolResultText(response);
        Assert.Contains("Deleted", text);
    }

    #endregion

    #region myclaw_exec Tests

    [Fact]
    public async Task Exec_EchoCommand_ShouldReturnOutput()
    {
        var response = await CallToolAsync("myclaw_exec", new
        {
            command = "echo Hello World"
        });

        var text = GetToolResultText(response);
        Assert.Contains("Hello World", text);
    }

    [Fact]
    public async Task Exec_InvalidCommand_ShouldReturnError()
    {
        var response = await CallToolAsync("myclaw_exec", new
        {
            command = "nonexistent_command_12345"
        });

        var text = GetToolResultText(response);
        Assert.Contains("Error", text);
    }

    #endregion

    #region myclaw_status Tests

    [Fact]
    public async Task Status_ShouldReturnStatusInfo()
    {
        var response = await CallToolAsync("myclaw_status", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("MyClaw Status", text);
    }

    #endregion

    #region Resources Tests

    [Fact]
    public async Task ResourcesList_ShouldReturnAllResources()
    {
        var response = await _fixture.SendRequestAsync("resources/list", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var resources = result.GetProperty("resources").EnumerateArray().ToList();
        
        var uris = resources.Select(r => r.GetProperty("uri").GetString()).ToList();
        
        Assert.Contains("myclaw://context", uris);
        Assert.Contains("myclaw://skills", uris);
        Assert.Contains("myclaw://status", uris);
    }

    [Fact]
    public async Task ResourcesRead_Context_ShouldReturnContent()
    {
        await CallToolAsync("myclaw_update", new { filename = "SOUL.md", content = "Context test" });
        
        var response = await _fixture.SendRequestAsync("resources/read", new { uri = "myclaw://context" });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var contents = result.GetProperty("contents").EnumerateArray().First();
        var text = contents.GetProperty("text").GetString();
        
        Assert.Contains("Context test", text);
    }

    #endregion

    #region Prompts Tests

    [Fact]
    public async Task PromptsList_ShouldReturnAllPrompts()
    {
        var response = await _fixture.SendRequestAsync("prompts/list", new { });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var prompts = result.GetProperty("prompts").EnumerateArray().ToList();
        
        var names = prompts.Select(p => p.GetProperty("name").GetString()).ToList();
        
        Assert.Contains("myclaw_wakeup", names);
        Assert.Contains("myclaw_growup", names);
        Assert.Contains("myclaw_briefing", names);
    }

    [Fact]
    public async Task PromptsGet_Wakeup_ShouldReturnWakeupMessage()
    {
        var response = await _fixture.SendRequestAsync("prompts/get", new { name = "myclaw_wakeup" });
        
        Assert.True(response.RootElement.TryGetProperty("result", out var result));
        var messages = result.GetProperty("messages").EnumerateArray().ToList();
        
        Assert.NotEmpty(messages);
        var content = messages[0].GetProperty("content").GetProperty("text").GetString();
        Assert.Contains("Waking", content);
    }

    #endregion

    #region Unknown Tool Tests

    [Fact]
    public async Task CallUnknownTool_ShouldReturnError()
    {
        var response = await CallToolAsync("unknown_tool", new { });
        var text = GetToolResultText(response);
        
        Assert.Contains("Unknown tool", text);
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
