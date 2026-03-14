using MyClaw.Core.Ribosome;

namespace MyClaw.Core.Tests.Ribosome;

public class RibosomeLoaderTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _templatesDir;

    public RibosomeLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"ribosome_test_{Guid.NewGuid()}");
        _templatesDir = Path.Combine(_tempDir, "templates");
        Directory.CreateDirectory(_tempDir);
        Directory.CreateDirectory(_templatesDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    #region LoadInstinctsAsync Tests

    [Fact]
    public async Task LoadInstinctsAsync_ShouldReturnDefaultInstincts_WhenNoFiles()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var instincts = await loader.LoadInstinctsAsync();

        Assert.NotEmpty(instincts);
        Assert.Contains("myclaw_update", instincts.Keys);
        Assert.Contains("myclaw_note", instincts.Keys);
        Assert.Contains("myclaw_read", instincts.Keys);
    }

    [Fact]
    public async Task LoadInstinctsAsync_ShouldLoadFromUserDirectory()
    {
        var userRibosome = @"{
          ""type"": ""ribosome"",
          ""version"": ""1.0.0"",
          ""description"": ""User RIBOSOME"",
          ""instincts"": {
            ""myclaw_custom"": {
              ""handler"": ""CustomHandler"",
              ""description"": ""Custom tool""
            }
          }
        }";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "RIBOSOME.json"), userRibosome);

        var loader = new RibosomeLoader(_tempDir, _templatesDir);
        var instincts = await loader.LoadInstinctsAsync();

        Assert.Contains("myclaw_custom", instincts.Keys);
        Assert.Equal("CustomHandler", instincts["myclaw_custom"].Handler);
    }

    [Fact]
    public async Task LoadInstinctsAsync_ShouldLoadFromTemplates_WhenNoUserFile()
    {
        var templateRibosome = @"{
          ""type"": ""ribosome"",
          ""version"": ""1.0.0"",
          ""description"": ""Template RIBOSOME"",
          ""instincts"": {
            ""myclaw_template"": {
              ""handler"": ""TemplateHandler"",
              ""description"": ""Template tool""
            }
          }
        }";
        await File.WriteAllTextAsync(Path.Combine(_templatesDir, "RIBOSOME.json"), templateRibosome);

        var loader = new RibosomeLoader(_tempDir, _templatesDir);
        var instincts = await loader.LoadInstinctsAsync();

        Assert.Contains("myclaw_template", instincts.Keys);
        Assert.Equal("TemplateHandler", instincts["myclaw_template"].Handler);
    }

    #endregion

    #region GetInstinctAsync Tests

    [Fact]
    public async Task GetInstinctAsync_ShouldReturnCorrectInstinct()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var instinct = await loader.GetInstinctAsync("myclaw_update");

        Assert.NotNull(instinct);
        Assert.Equal("UpdateDNA", instinct.Handler);
    }

    [Fact]
    public async Task GetInstinctAsync_ShouldReturnNull_WhenNotFound()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var instinct = await loader.GetInstinctAsync("nonexistent");

        Assert.Null(instinct);
    }

    #endregion

    #region GetHandlerAsync Tests

    [Fact]
    public async Task GetHandlerAsync_ShouldReturnCorrectHandler()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var handler = await loader.GetHandlerAsync("myclaw_note");

        Assert.Equal("Note", handler);
    }

    [Fact]
    public async Task GetHandlerAsync_ShouldReturnNull_WhenNotFound()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var handler = await loader.GetHandlerAsync("nonexistent");

        Assert.Null(handler);
    }

    #endregion

    #region GetToolNamesAsync Tests

    [Fact]
    public async Task GetToolNamesAsync_ShouldReturnAllToolNames()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var names = await loader.GetToolNamesAsync();

        Assert.NotEmpty(names);
        Assert.Contains("myclaw_update", names);
        Assert.Contains("myclaw_note", names);
        Assert.Contains("myclaw_read", names);
        Assert.Contains("myclaw_exec", names);
    }

    #endregion

    #region Cache Tests

    [Fact]
    public async Task Cache_ShouldReturnSameInstance_WithinTtl()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var config1 = await loader.LoadConfigAsync();
        var config2 = await loader.LoadConfigAsync();

        Assert.Same(config1, config2);
    }

    [Fact]
    public async Task InvalidateCache_ShouldForceReload()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var config1 = await loader.LoadConfigAsync();
        loader.InvalidateCache();
        var config2 = await loader.LoadConfigAsync();

        Assert.NotSame(config1, config2);
    }

    #endregion

    #region Input Schema Tests

    [Fact]
    public async Task InputSchema_ShouldParseProperties()
    {
        var ribosome = @"{
          ""type"": ""ribosome"",
          ""version"": ""1.0.0"",
          ""description"": ""Test"",
          ""instincts"": {
            ""test_tool"": {
              ""handler"": ""Test"",
              ""description"": ""Test tool"",
              ""inputSchema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""param1"": {
                    ""type"": ""string"",
                    ""description"": ""First parameter""
                  },
                  ""param2"": {
                    ""type"": ""integer""
                  }
                },
                ""required"": [""param1""]
              }
            }
          }
        }";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "RIBOSOME.json"), ribosome);

        var loader = new RibosomeLoader(_tempDir, _templatesDir);
        var instinct = await loader.GetInstinctAsync("test_tool");

        Assert.NotNull(instinct);
        Assert.NotNull(instinct.InputSchema);
        Assert.Equal("object", instinct.InputSchema.Type);
        Assert.NotNull(instinct.InputSchema.Properties);
        Assert.Equal(2, instinct.InputSchema.Properties.Count);
        Assert.Contains("param1", instinct.InputSchema.Required);
    }

    [Fact]
    public async Task InputSchema_ShouldParseEnumValues()
    {
        var ribosome = @"{
          ""type"": ""ribosome"",
          ""version"": ""1.0.0"",
          ""description"": ""Test"",
          ""instincts"": {
            ""enum_tool"": {
              ""handler"": ""EnumTest"",
              ""description"": ""Enum test"",
              ""inputSchema"": {
                ""type"": ""object"",
                ""properties"": {
                  ""action"": {
                    ""type"": ""string"",
                    ""enum"": [""create"", ""list"", ""delete""]
                  }
                },
                ""required"": [""action""]
              }
            }
          }
        }";
        await File.WriteAllTextAsync(Path.Combine(_tempDir, "RIBOSOME.json"), ribosome);

        var loader = new RibosomeLoader(_tempDir, _templatesDir);
        var instinct = await loader.GetInstinctAsync("enum_tool");

        Assert.NotNull(instinct);
        Assert.NotNull(instinct.InputSchema?.Properties);
        var actionProp = instinct.InputSchema.Properties["action"];
        Assert.NotNull(actionProp.Enum);
        Assert.Equal(3, actionProp.Enum.Count);
        Assert.Contains("create", actionProp.Enum);
        Assert.Contains("list", actionProp.Enum);
        Assert.Contains("delete", actionProp.Enum);
    }

    #endregion

    #region GetMcpToolsAsync Tests

    [Fact]
    public async Task GetMcpToolsAsync_ShouldReturnAllTools()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var tools = await loader.GetMcpToolsAsync();

        Assert.NotEmpty(tools);
        Assert.Contains(tools, t => t.Name == "myclaw_update");
        Assert.Contains(tools, t => t.Name == "myclaw_note");
    }

    [Fact]
    public async Task GetMcpToolsAsync_ShouldHaveValidInputSchema()
    {
        var loader = new RibosomeLoader(_tempDir, _templatesDir);

        var tools = await loader.GetMcpToolsAsync();
        var updateTool = tools.First(t => t.Name == "myclaw_update");

        Assert.NotNull(updateTool.InputSchema);
        Assert.Contains("type", updateTool.InputSchema.ToJsonString());
    }

    #endregion
}
