using System.Reactive.Linq;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Agent;

public class MyClawAgent
{
    private readonly EnhancedReActAgent _agent;
    private readonly MyClawConfiguration _config;
    private readonly MemoryStore _memoryStore;
    private readonly AgentPromptContextBuilder _promptContextBuilder;

    public MyClawAgent(
        MyClawConfiguration config,
        IModel model,
        MemoryStore memoryStore,
        SkillManager? skillManager = null)
    {
        _config = config;
        _memoryStore = memoryStore;
        _promptContextBuilder = new AgentPromptContextBuilder(config, memoryStore);

        var systemPrompt = _promptContextBuilder.BuildSystemPrompt();
        if (skillManager != null)
        {
            var hookContext = skillManager.BuildHookContext(SkillHookType.Boot);
            if (!string.IsNullOrWhiteSpace(hookContext))
            {
                systemPrompt = $"{systemPrompt}\n\n{hookContext}";
            }
        }

        var builder = EnhancedReActAgent.Builder()
            .Name("MyClaw")
            .Model(model)
            .SysPrompt(systemPrompt)
            .MaxIterations(config.Agent.MaxToolIterations)
            .Verbose(config.Agent.Verbose);

        if (skillManager != null)
        {
            foreach (var skill in skillManager.LoadedSkills)
            {
                builder.AddTool(new SkillTool(skill));
            }
        }

        _agent = builder.Build();
    }

    public async Task<string> ChatAsync(string message, string sessionId = "default")
    {
        var msg = Msg.Builder()
            .Role("user")
            .TextContent(message)
            .AddMetadata("session_id", sessionId)
            .Build();

        var response = await _agent.Call(msg).FirstAsync();
        return response.GetTextContent() ?? "无响应";
    }
}
