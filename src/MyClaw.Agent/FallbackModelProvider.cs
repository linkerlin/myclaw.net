using System.Reactive.Linq;
using AgentScope.Core;
using AgentScope.Core.Message;
using AgentScope.Core.Model;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Agent;

/// <summary>
/// 自动降级模型提供者
/// 当主要 Provider 失败时，自动切换到备用 Provider
/// </summary>
public class FallbackModelProvider
{
    private readonly MyClawConfiguration _config;
    private readonly List<ProviderCandidate> _candidates;
    private int _currentIndex;
    private IModel? _currentModel;

    public FallbackModelProvider(MyClawConfiguration config)
    {
        _config = config;
        _candidates = BuildProviderCandidates();
        _currentIndex = 0;
    }

    /// <summary>
    /// 当前使用的 Provider 信息
    /// </summary>
    public string CurrentProvider => _currentIndex < _candidates.Count 
        ? _candidates[_currentIndex].Name 
        : "unknown";

    /// <summary>
    /// 获取可用候选者列表（用于显示）
    /// </summary>
    public IReadOnlyList<string> AvailableProviders => 
        _candidates.Select(c => c.Name).ToList().AsReadOnly();

    /// <summary>
    /// 获取当前模型实例，如果失败则尝试降级
    /// </summary>
    public async Task<(IModel Model, string ProviderName)> GetModelAsync(
        Func<IModel, Task<bool>>? testFunc = null)
    {
        while (_currentIndex < _candidates.Count)
        {
            var candidate = _candidates[_currentIndex];
            
            try
            {
                Console.WriteLine($"[Fallback] 尝试使用 {candidate.Name}...");
                Console.WriteLine($"[Fallback] Config: Type={candidate.Config.Type}, Model={candidate.Config.Model}, BaseUrl={candidate.Config.BaseUrl}");
                var model = ModelFactory.Create(candidate.Config);
                
                // 如果提供了测试函数，先测试连接
                if (testFunc != null)
                {
                    var isWorking = await testFunc(model);
                    if (!isWorking)
                    {
                        Console.WriteLine($"[Fallback] {candidate.Name} 测试失败，切换到下一个...");
                        _currentIndex++;
                        continue;
                    }
                }
                
                _currentModel = model;
                Console.WriteLine($"[Fallback] 成功使用 {candidate.Name}");
                return (model, candidate.Name);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Fallback] {candidate.Name} 初始化失败: {ex.Message}");
                _currentIndex++;
            }
        }

        throw new InvalidOperationException(
            "所有 Provider 均不可用。请检查以下环境变量之一是否设置：\n" +
            "- OPENAI_API_KEY\n" +
            "- DEEPSEEK_API_KEY\n" +
            "- ANTHROPIC_API_KEY\n" +
            "- MYCLAW_API_KEY"
        );
    }

    /// <summary>
    /// 当调用失败时，尝试切换到下一个 Provider
    /// </summary>
    public async Task<(IModel Model, string ProviderName)?> TryFallbackAsync(
        Exception lastError,
        Func<IModel, Task<bool>>? testFunc = null)
    {
        Console.WriteLine($"[Fallback] 当前 Provider 失败: {lastError.Message}");
        _currentIndex++;
        
        if (_currentIndex >= _candidates.Count)
        {
            Console.WriteLine("[Fallback] 没有更多备用 Provider");
            return null;
        }

        return await GetModelAsync(testFunc);
    }

    /// <summary>
    /// 重置到第一个 Provider
    /// </summary>
    public void Reset()
    {
        _currentIndex = 0;
    }

    /// <summary>
    /// 构建 Provider 候选列表（按优先级排序）
    /// </summary>
    private List<ProviderCandidate> BuildProviderCandidates()
    {
        var candidates = new List<ProviderCandidate>();

        // 1. 当前配置的 Provider（最高优先级）
        if (!string.IsNullOrEmpty(_config.Provider.ApiKey))
        {
            candidates.Add(new ProviderCandidate
            {
                Name = _config.Provider.Type,
                Config = _config.Provider
            });
        }

        // 2. OpenAI
        var openaiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrEmpty(openaiKey) && 
            _config.Provider.Type?.ToLowerInvariant() != "openai")
        {
            candidates.Add(new ProviderCandidate
            {
                Name = "openai",
                Config = new ProviderConfig
                {
                    Type = "openai",
                    ApiKey = openaiKey,
                    BaseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL") ?? "",
                    Model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "gpt-4o"
                }
            });
        }

        // 3. DeepSeek
        var deepseekKey = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
        if (!string.IsNullOrEmpty(deepseekKey) && 
            _config.Provider.Type?.ToLowerInvariant() != "deepseek")
        {
            candidates.Add(new ProviderCandidate
            {
                Name = "deepseek",
                Config = new ProviderConfig
                {
                    Type = "deepseek",
                    ApiKey = deepseekKey,
                    BaseUrl = "https://api.deepseek.com/v1",
                    Model = Environment.GetEnvironmentVariable("DEEPSEEK_MODEL") ?? "deepseek-chat"
                }
            });
        }

        // 4. Anthropic
        var anthropicKey = Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY") 
            ?? Environment.GetEnvironmentVariable("ANTHROPIC_AUTH_TOKEN");
        if (!string.IsNullOrEmpty(anthropicKey) && 
            _config.Provider.Type?.ToLowerInvariant() != "anthropic")
        {
            candidates.Add(new ProviderCandidate
            {
                Name = "anthropic",
                Config = new ProviderConfig
                {
                    Type = "anthropic",
                    ApiKey = anthropicKey,
                    BaseUrl = Environment.GetEnvironmentVariable("ANTHROPIC_BASE_URL") ?? "",
                    Model = Environment.GetEnvironmentVariable("ANTHROPIC_MODEL") ?? "claude-3-5-sonnet-20241022"
                }
            });
        }

        // 5. MYCLAW_API_KEY（通用）
        var myclawKey = Environment.GetEnvironmentVariable("MYCLAW_API_KEY");
        if (!string.IsNullOrEmpty(myclawKey) &&
            myclawKey != _config.Provider.ApiKey &&
            myclawKey != openaiKey &&
            myclawKey != deepseekKey &&
            myclawKey != anthropicKey)
        {
            candidates.Add(new ProviderCandidate
            {
                Name = "myclaw",
                Config = new ProviderConfig
                {
                    Type = _config.Provider.Type, // 使用当前配置的类型
                    ApiKey = myclawKey,
                    BaseUrl = Environment.GetEnvironmentVariable("MYCLAW_BASE_URL") ?? _config.Provider.BaseUrl,
                    Model = _config.Provider.Model
                }
            });
        }

        return candidates;
    }

    private class ProviderCandidate
    {
        public string Name { get; set; } = "";
        public ProviderConfig Config { get; set; } = new();
    }
}

/// <summary>
/// 支持自动降级的 Agent
/// </summary>
public class FallbackAgent
{
    private readonly FallbackModelProvider _fallbackProvider;
    private readonly MyClawConfiguration _config;
    private readonly MemoryStore _memoryStore;
    private readonly SkillManager? _skillManager;
    private EnhancedReActAgent? _agent;
    private IModel? _currentModel;

    public FallbackAgent(
        MyClawConfiguration config,
        MemoryStore memoryStore,
        SkillManager? skillManager = null)
    {
        _config = config;
        _memoryStore = memoryStore;
        _skillManager = skillManager;
        _fallbackProvider = new FallbackModelProvider(config);
    }

    /// <summary>
    /// 当前使用的 Provider 名称
    /// </summary>
    public string CurrentProvider => _fallbackProvider.CurrentProvider;

    /// <summary>
    /// 可用的 Provider 列表
    /// </summary>
    public IReadOnlyList<string> AvailableProviders => _fallbackProvider.AvailableProviders;

    /// <summary>
    /// 初始化 Agent（选择第一个可用的 Provider）
    /// </summary>
    public async Task InitializeAsync()
    {
        var (model, providerName) = await _fallbackProvider.GetModelAsync();
        _currentModel = model;
        BuildAgent(model);
        Console.WriteLine($"[FallbackAgent] 已初始化，使用 Provider: {providerName}");
    }

    /// <summary>
    /// 发送消息并获取回复，自动处理降级
    /// </summary>
    public async Task<string> ChatAsync(string message, string sessionId = "default")
    {
        if (_agent == null)
        {
            await InitializeAsync();
        }

        const int maxRetries = 3;
        int attempt = 0;

        while (attempt < maxRetries)
        {
            try
            {
                var msg = Msg.Builder()
                    .Role("user")
                    .TextContent(message)
                    .AddMetadata("session_id", sessionId)
                    .Build();

                var response = await _agent!.Call(msg).FirstAsync();
                var textContent = response.GetTextContent();
                Console.WriteLine($"[Debug] Response type: {response.GetType().Name}");
                Console.WriteLine($"[Debug] Text content: {textContent ?? "(null)"}");
                return textContent ?? "无响应";
            }
            catch (Exception ex) when (IsRecoverableError(ex))
            {
                attempt++;
                Console.WriteLine($"[FallbackAgent] 调用失败 (尝试 {attempt}/{maxRetries}): {ex.Message}");

                if (attempt >= maxRetries)
                {
                    // 尝试切换到下一个 Provider
                    var fallbackResult = await _fallbackProvider.TryFallbackAsync(ex);
                    if (fallbackResult != null)
                    {
                        _currentModel = fallbackResult.Value.Model;
                        BuildAgent(_currentModel);
                        attempt = 0; // 重置重试计数
                        Console.WriteLine($"[FallbackAgent] 已降级到: {fallbackResult.Value.ProviderName}");
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            "所有 Provider 均调用失败。最后一个错误: " + ex.Message, ex);
                    }
                }
            }
        }

        return "所有 Provider 均不可用";
    }

    private void BuildAgent(IModel model)
    {
        var systemPrompt = BuildSystemPrompt();

        var builder = EnhancedReActAgent.Builder()
            .Name("MyClaw")
            .Model(model)
            .SysPrompt(systemPrompt)
            .MaxIterations(_config.Agent.MaxToolIterations)
            .Verbose(_config.Agent.Verbose);

        if (_skillManager != null)
        {
            foreach (var skill in _skillManager.LoadedSkills)
            {
                builder.AddTool(new SkillTool(skill));
            }
        }

        _agent = builder.Build();
    }

    private string BuildSystemPrompt()
    {
        var parts = new List<string>();

        var workspace = _config.Agent.Workspace;

        var agentsPath = Path.Combine(workspace, "AGENTS.md");
        if (File.Exists(agentsPath))
        {
            parts.Add(File.ReadAllText(agentsPath));
        }

        var soulPath = Path.Combine(workspace, "SOUL.md");
        if (File.Exists(soulPath))
        {
            parts.Add(File.ReadAllText(soulPath));
        }

        var heartbeatPath = Path.Combine(workspace, "HEARTBEAT.md");
        if (File.Exists(heartbeatPath))
        {
            parts.Add("## 心跳任务\n" + File.ReadAllText(heartbeatPath));
        }

        var memoryContext = _memoryStore.GetMemoryContext();
        if (!string.IsNullOrEmpty(memoryContext))
        {
            parts.Add("## 记忆上下文\n" + memoryContext);
        }

        parts.Add($@"
你是 MyClaw，一个个人 AI 助手。
当前使用的模型提供者: {CurrentProvider}

你可以使用以下工具来完成任务：
- Skills: 各种专业领域的技能助手
- Calculator: 数学计算
- GetTime: 获取当前时间

请用中文或用户使用的语言回复。
");

        return string.Join("\n\n", parts);
    }

    /// <summary>
    /// 判断错误是否可恢复（可以尝试降级）
    /// </summary>
    private static bool IsRecoverableError(Exception ex)
    {
        var errorMessage = ex.Message.ToLowerInvariant();
        
        // API 调用错误
        if (errorMessage.Contains("notfound") ||
            errorMessage.Contains("unauthorized") ||
            errorMessage.Contains("forbidden") ||
            errorMessage.Contains("rate limit") ||
            errorMessage.Contains("timeout") ||
            errorMessage.Contains("connection") ||
            errorMessage.Contains("network") ||
            errorMessage.Contains("api error") ||
            errorMessage.Contains("http") ||
            errorMessage.Contains("json"))
        {
            return true;
        }

        // AgentScope 特定错误
        if (errorMessage.Contains("reasoning error") ||
            errorMessage.Contains("model") ||
            errorMessage.Contains("provider"))
        {
            return true;
        }

        return false;
    }
}
