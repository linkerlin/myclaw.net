using AgentScope.Core;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;

namespace MyClaw.Agent;

public static class ModelFactory
{
    public static IModel Create(ProviderConfig config)
    {
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            throw new InvalidOperationException("API key is required");
        }

        var provider = config.Type?.ToLowerInvariant() ?? "openai";
        var modelName = config.Model ?? AgentScope.Core.ModelFactoryExtensions.GetDefaultModel(provider);
        
        Console.WriteLine($"[ModelFactory] Creating model: provider={provider}, model={modelName}");
        
        return AgentScope.Core.ModelFactory.Create(
            provider: provider,
            modelName: modelName,
            apiKey: config.ApiKey,
            baseUrl: config.BaseUrl
        );
    }
}
