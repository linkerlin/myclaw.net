using MyClaw.Agent;
using MyClaw.Core.Configuration;
using AgentScope.Core.Model;
using AgentScope.Core.Model.Anthropic;
using AgentScope.Core.Model.OpenAI;
using AgentScope.Core.Model.DeepSeek;

namespace MyClaw.Core.Tests.Agent;

public class ModelFactoryTests
{
    [Fact]
    public void Create_AnthropicProvider_ShouldCreateModel()
    {
        var config = new ProviderConfig
        {
            Type = "anthropic",
            ApiKey = "test-key-anthropic"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.IsType<AnthropicModel>(model);
    }

    [Fact]
    public void Create_OpenAIProvider_ShouldCreateModel()
    {
        var config = new ProviderConfig
        {
            Type = "openai",
            ApiKey = "test-key-openai"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.IsType<OpenAIModel>(model);
    }

    [Fact]
    public void Create_DeepSeekProvider_ShouldCreateModel()
    {
        var config = new ProviderConfig
        {
            Type = "deepseek",
            ApiKey = "test-key-deepseek"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.IsType<DeepSeekModel>(model);
    }

    [Fact]
    public void Create_WithCustomModel_ShouldUseCustomModel()
    {
        var config = new ProviderConfig
        {
            Type = "anthropic",
            Model = "claude-3-opus-20240229",
            ApiKey = "test-key"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
    }

    [Fact]
    public void Create_WithCustomBaseUrl_ShouldUseCustomBaseUrl()
    {
        var config = new ProviderConfig
        {
            Type = "openai",
            ApiKey = "test-key",
            BaseUrl = "https://custom.endpoint.com/v1"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
    }

    [Fact]
    public void Create_MissingApiKey_ShouldThrowException()
    {
        var config = new ProviderConfig
        {
            Type = "anthropic",
            ApiKey = ""
        };

        Assert.Throws<InvalidOperationException>(() => ModelFactory.Create(config));
    }

    [Fact]
    public void Create_UnsupportedProvider_ShouldThrowException()
    {
        var config = new ProviderConfig
        {
            Type = "unsupported-provider",
            ApiKey = "test-key"
        };

        Assert.Throws<NotSupportedException>(() => ModelFactory.Create(config));
    }

    [Fact]
    public void Create_DefaultOpenAI_WhenTypeIsNull_ShouldUseOpenAI()
    {
        var config = new ProviderConfig
        {
            Type = "",
            ApiKey = "test-key"
        };

        var model = ModelFactory.Create(config);

        Assert.NotNull(model);
        Assert.IsType<OpenAIModel>(model);
    }
}
