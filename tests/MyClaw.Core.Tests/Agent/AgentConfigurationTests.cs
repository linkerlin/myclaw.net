using MyClaw.Core.Configuration;

namespace MyClaw.Core.Tests.Agent;

public class AgentConfigurationTests
{
    [Fact]
    public void AgentConfig_DefaultValues_ShouldHaveCorrectDefaults()
    {
        var config = new AgentConfig();

        Assert.Equal(MyClawConfiguration.DefaultModel, config.Model);
        Assert.Equal(MyClawConfiguration.DefaultMaxTokens, config.MaxTokens);
        Assert.Equal(MyClawConfiguration.DefaultTemperature, config.Temperature);
        Assert.Equal(MyClawConfiguration.DefaultMaxToolIterations, config.MaxToolIterations);
        Assert.False(config.Verbose);
    }

    [Fact]
    public void AgentConfig_CustomValues_ShouldBeSetCorrectly()
    {
        var config = new AgentConfig
        {
            Workspace = "/custom/workspace",
            Model = "gpt-4-turbo",
            MaxTokens = 4096,
            Temperature = 0.5,
            MaxToolIterations = 5,
            Verbose = true
        };

        Assert.Equal("/custom/workspace", config.Workspace);
        Assert.Equal("gpt-4-turbo", config.Model);
        Assert.Equal(4096, config.MaxTokens);
        Assert.Equal(0.5, config.Temperature);
        Assert.Equal(5, config.MaxToolIterations);
        Assert.True(config.Verbose);
    }

    [Fact]
    public void ProviderConfig_DefaultValues_ShouldHaveCorrectDefaults()
    {
        var config = new ProviderConfig();

        Assert.Equal("openai", config.Type);
        Assert.Equal("", config.ApiKey);
        Assert.Equal("", config.BaseUrl);
        Assert.Null(config.Model);
    }

    [Fact]
    public void ProviderConfig_CustomValues_ShouldBeSetCorrectly()
    {
        var config = new ProviderConfig
        {
            Type = "deepseek",
            Model = "deepseek-chat",
            ApiKey = "sk-test123",
            BaseUrl = "https://api.deepseek.com"
        };

        Assert.Equal("deepseek", config.Type);
        Assert.Equal("deepseek-chat", config.Model);
        Assert.Equal("sk-test123", config.ApiKey);
        Assert.Equal("https://api.deepseek.com", config.BaseUrl);
    }

    [Fact]
    public void MyClawConfiguration_Default_ShouldIncludeAgentConfig()
    {
        var config = MyClawConfiguration.Default();

        Assert.NotNull(config.Agent);
        Assert.NotNull(config.Provider);
        Assert.NotEmpty(config.Agent.Workspace);
    }
}
