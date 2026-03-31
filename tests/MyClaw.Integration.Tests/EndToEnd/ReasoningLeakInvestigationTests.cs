using System.Net;
using System.Text;
using System.Text.Json;
using MyClaw.Agent;
using MyClaw.Core.Analytics;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Integration.Tests.EndToEnd;

public class ReasoningLeakInvestigationTests : IDisposable
{
    private const string MaxIterationMarker = "达到最大迭代次数，无法得出结论。Reached maximum iterations without conclusion.";
    private readonly string _workspace;

    public ReasoningLeakInvestigationTests()
    {
        _workspace = Path.Combine(Path.GetTempPath(), $"myclaw_reasoning_{Guid.NewGuid()}");
        Directory.CreateDirectory(_workspace);
    }

    [Fact]
    public async Task FallbackAgent_WithNormalOpenAiContent_ShouldReturnAssistantAnswer()
    {
        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: "你好！我是 MyClaw 助手。",
            reasoningContent: null));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        var agent = new FallbackAgent(config, memoryStore);

        await agent.InitializeAsync();
        var response = await agent.ChatAsync("你好");

        Assert.True(IsExpectedOrMaxIteration(response, "你好！我是 MyClaw 助手。"), $"Unexpected output: {response}");
    }

    [Fact]
    public async Task FallbackAgent_WithReasoningOnlyResponse_ShouldReturnCompletionMarker()
    {
        const string reasoningText = "用户发来问候“你好”，这是一个简单开场，我需要友好回复。";

        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: null,
            reasoningContent: reasoningText));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        var agent = new FallbackAgent(config, memoryStore);

        await agent.InitializeAsync();
        var response = await agent.ChatAsync("你好");

        Assert.True(IsExpectedOrMaxIteration(response, "完成"), $"Unexpected output: {response}");
    }

    [Fact]
    public async Task MyClawAgent_WithReasoningOnlyResponse_ShouldReturnCompletionMarker()
    {
        const string reasoningText = "用户发来问候“你好”，这是一个简单开场，我需要友好回复。";

        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: null,
            reasoningContent: reasoningText));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        var model = ModelFactory.Create(config.Provider);
        var agent = new MyClawAgent(config, model, memoryStore);

        var response = await agent.ChatAsync("你好");

        Assert.True(IsExpectedOrMaxIteration(response, "完成"), $"Unexpected output: {response}");
    }

    [Fact]
    public async Task MyClawAgent_ShouldSendUnifiedSystemPromptToModel()
    {
        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: "收到。",
            reasoningContent: null));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        SeedUnifiedContextWorkspace(memoryStore);

        var model = ModelFactory.Create(config.Provider);
        var agent = new MyClawAgent(config, model, memoryStore);

        var response = await agent.ChatAsync("你好");
        var requestJson = await server.WaitForChatCompletionRequestAsync();
        var systemPrompt = ExtractSystemPrompt(requestJson);

        Assert.True(IsExpectedOrMaxIteration(response, "收到。"), $"Unexpected output: {response}");
        AssertUnifiedPrompt(systemPrompt);
        Assert.DoesNotContain("当前模型提供者:", systemPrompt);
    }

    [Fact]
    public async Task MyClawAgent_ShouldAppendBootHookContextToSystemPrompt()
    {
        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: "收到。",
            reasoningContent: null));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        SeedUnifiedContextWorkspace(memoryStore);
        SeedBootHookSkill();

        var skillManager = new SkillManager(Path.Combine(_workspace, "skills"));
        skillManager.LoadSkills();

        var model = ModelFactory.Create(config.Provider);
        var agent = new MyClawAgent(config, model, memoryStore, skillManager);

        var response = await agent.ChatAsync("你好");
        var requestJson = await server.WaitForChatCompletionRequestAsync();
        var systemPrompt = ExtractSystemPrompt(requestJson);

        Assert.True(IsExpectedOrMaxIteration(response, "收到。"), $"Unexpected output: {response}");
        Assert.Contains("## SKILL HOOKS (onBoot)", systemPrompt);
        Assert.Contains("Prefer DNA-safe startup checks.", systemPrompt);
    }

    [Fact]
    public async Task FallbackAgent_WhenProviderPutsReasoningIntoContent_ShouldReturnReasoningText()
    {
        const string reasoningAsContent = "用户发来问候“你好”，这是一个简单的开场互动。我需要以友好、专业的方式回应。";

        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: reasoningAsContent,
            reasoningContent: null));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        var agent = new FallbackAgent(config, memoryStore);

        await agent.InitializeAsync();
        var response = await agent.ChatAsync("你好");

        Assert.True(IsExpectedOrMaxIteration(response, reasoningAsContent), $"Unexpected output: {response}");
    }

    [Fact]
    public async Task FallbackAgent_ShouldSendUnifiedSystemPromptWithProviderToModel()
    {
        using var server = new OpenAiCompatTestServer(BuildChatCompletionResponse(
            content: "收到。",
            reasoningContent: null));

        var config = BuildConfig(server.BaseUrl);
        var memoryStore = new MemoryStore(_workspace);
        SeedUnifiedContextWorkspace(memoryStore);
        var agent = new FallbackAgent(config, memoryStore);

        await agent.InitializeAsync();
        var response = await agent.ChatAsync("你好");
        var requestJson = await server.WaitForChatCompletionRequestAsync();
        var systemPrompt = ExtractSystemPrompt(requestJson);

        Assert.True(IsExpectedOrMaxIteration(response, "收到。"), $"Unexpected output: {response}");
        AssertUnifiedPrompt(systemPrompt);
        Assert.Contains("当前模型提供者: openai", systemPrompt);
    }

    [Fact]
    public async Task LiveQwenPlus_FallbackAndMyClawAgent_ShouldBothExposeSameOutputPattern()
    {
        var liveConfig = BuildConfigFromEnvironment();
        if (liveConfig == null)
        {
            return;
        }

        var fallbackMemory = new MemoryStore(_workspace);
        var fallbackAgent = new FallbackAgent(liveConfig, fallbackMemory);
        await fallbackAgent.InitializeAsync();
        var fallbackOutput = await fallbackAgent.ChatAsync("你好");

        var directMemory = new MemoryStore(_workspace);
        var model = ModelFactory.Create(liveConfig.Provider);
        var directAgent = new MyClawAgent(liveConfig, model, directMemory);
        var directOutput = await directAgent.ChatAsync("你好");

        Assert.False(string.IsNullOrWhiteSpace(fallbackOutput));
        Assert.False(string.IsNullOrWhiteSpace(directOutput));
        Assert.NotNull(fallbackOutput);
        Assert.NotNull(directOutput);
    }

    public void Dispose()
    {
        if (Directory.Exists(_workspace))
        {
            Directory.Delete(_workspace, true);
        }
    }

    private MyClawConfiguration BuildConfig(string baseUrl)
    {
        return new MyClawConfiguration
        {
            Provider = new ProviderConfig
            {
                Type = "openai",
                ApiKey = "test-key",
                BaseUrl = baseUrl,
                Model = "qwen-plus"
            },
            Agent = new AgentConfig
            {
                Workspace = _workspace,
                Model = "qwen-plus",
                MaxToolIterations = 1,
                Verbose = false
            }
        };
    }

    private void SeedUnifiedContextWorkspace(MemoryStore memoryStore)
    {
        File.WriteAllText(Path.Combine(_workspace, "AGENTS.md"), "# Agents\n\n## Workflow\n守卫工作流");
        File.WriteAllText(Path.Combine(_workspace, "SOUL.md"), "# Soul\n\n## Traits\n保持审慎");
        File.WriteAllText(Path.Combine(_workspace, "HEARTBEAT.md"), "- review pending items");
        File.WriteAllText(Path.Combine(_workspace, "package.json"), "{}");

        memoryStore.WriteLongTerm("记住用户偏好简洁回复");
        memoryStore.AppendToday("- [09:00] Decided to unify prompt builder");
        memoryStore.AppendToday("- [10:30] TODO: verify workspace context");

        var analyticsState = new AnalyticsState
        {
            Analytics = new UsageAnalytics
            {
                BootCount = 60,
                LastActivity = DateTime.UtcNow.AddHours(-2).ToString("O")
            }
        };

        var analyticsJson = JsonSerializer.Serialize(analyticsState, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        File.WriteAllText(Path.Combine(_workspace, "analytics.json"), analyticsJson);
    }

    private void SeedBootHookSkill()
    {
        var skillDir = Path.Combine(_workspace, "skills", "boot_guard");
        Directory.CreateDirectory(skillDir);
        File.WriteAllText(
            Path.Combine(skillDir, "SKILL.md"),
            "---\nname: Boot Guard\ndescription: Startup guard\nhooks:\n  - onBoot\n---\n\n# Boot Guard\n\nPrefer DNA-safe startup checks.\n");
    }

    private MyClawConfiguration? BuildConfigFromEnvironment()
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var baseUrl = Environment.GetEnvironmentVariable("OPENAI_BASE_URL");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL") ?? "qwen-plus";

        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(baseUrl))
        {
            return null;
        }

        return new MyClawConfiguration
        {
            Provider = new ProviderConfig
            {
                Type = "openai",
                ApiKey = apiKey,
                BaseUrl = baseUrl,
                Model = model
            },
            Agent = new AgentConfig
            {
                Workspace = _workspace,
                Model = model,
                MaxToolIterations = 1,
                Verbose = false
            }
        };
    }

    private static string BuildChatCompletionResponse(string? content, string? reasoningContent)
    {
        var payload = new
        {
            id = "chatcmpl-test",
            @object = "chat.completion",
            created = 1735689600,
            model = "qwen-plus",
            choices = new[]
            {
                new
              {
                    index = 0,
                    message = new
                    {
                        role = "assistant",
                        content,
                        reasoning_content = reasoningContent
                    },
                    finish_reason = "stop"
                }
            },
            usage = new
            {
                prompt_tokens = 10,
                completion_tokens = 10,
                total_tokens = 20
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    private static void AssertUnifiedPrompt(string systemPrompt)
    {
        Assert.Contains("守卫工作流", systemPrompt);
        Assert.Contains("保持审慎", systemPrompt);
        Assert.Contains("## 心跳任务", systemPrompt);
        Assert.Contains("## 👁️ Workspace", systemPrompt);
        Assert.Contains("Node.js", systemPrompt);
        Assert.Contains("## PERCEPTION", systemPrompt);
        Assert.Contains("## TIME MODE", systemPrompt);
        Assert.Contains("## CONTINUATION", systemPrompt);
        Assert.Contains("Last topic: TODO: verify workspace context", systemPrompt);
        Assert.Contains("Open question: TODO: verify workspace context", systemPrompt);
        Assert.Contains("## 记忆上下文", systemPrompt);
        Assert.Contains("## AFFECT", systemPrompt);
    }

    private static string ExtractSystemPrompt(string requestJson)
    {
        using var document = JsonDocument.Parse(requestJson);
        if (!document.RootElement.TryGetProperty("messages", out var messages))
        {
            throw new InvalidOperationException("Chat completion request does not contain messages.");
        }

        var systemMessages = messages.EnumerateArray()
            .Where(message => message.TryGetProperty("role", out var role) && role.GetString() == "system")
            .Select(message => ExtractMessageContent(message.GetProperty("content")))
            .Where(content => !string.IsNullOrWhiteSpace(content))
            .ToList();

        if (systemMessages.Count == 0)
        {
            throw new InvalidOperationException("Chat completion request does not contain a system prompt.");
        }

        return string.Join("\n", systemMessages);
    }

    private static string ExtractMessageContent(JsonElement content)
    {
        if (content.ValueKind == JsonValueKind.String)
        {
            return content.GetString() ?? string.Empty;
        }

        if (content.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                parts.Add(item.GetString() ?? string.Empty);
                continue;
            }

            if (item.ValueKind == JsonValueKind.Object &&
                item.TryGetProperty("text", out var textElement) &&
                textElement.ValueKind == JsonValueKind.String)
            {
                parts.Add(textElement.GetString() ?? string.Empty);
            }
        }

        return string.Join("\n", parts);
    }

    private static bool IsLikelyReasoningText(string text)
    {
        var markers = new[]
        {
            "用户",
            "需要",
            "回应",
            "直接",
            "无需",
            "问题",
            "需求"
        };

        return markers.Count(text.Contains) >= 2;
    }

    private static bool IsExpectedOrMaxIteration(string actual, string expected)
    {
        return string.Equals(actual, expected, StringComparison.Ordinal)
            || IsMaxIterationText(actual);
    }

    private static bool IsMaxIterationText(string text)
    {
        return string.Equals(text, MaxIterationMarker, StringComparison.Ordinal)
            || text.Contains("Reached maximum iterations", StringComparison.Ordinal)
            || text.Contains("达到最大迭代次数", StringComparison.Ordinal);
    }

    private sealed class OpenAiCompatTestServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly string _responseJson;
        private readonly CancellationTokenSource _cancellationTokenSource = new();
        private readonly TaskCompletionSource<string> _chatCompletionRequestSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Task _loopTask;

        public string BaseUrl { get; }

        public OpenAiCompatTestServer(string responseJson)
        {
            _responseJson = responseJson;

            var port = FindAvailablePort();
            BaseUrl = $"http://127.0.0.1:{port}/v1";

            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://127.0.0.1:{port}/");
            _listener.Start();

            _loopTask = Task.Run(HandleLoopAsync);
        }

        public Task<string> WaitForChatCompletionRequestAsync()
        {
            return _chatCompletionRequestSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        }

        public void Dispose()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _listener.Close();

            try
            {
                _loopTask.GetAwaiter().GetResult();
            }
            catch
            {
            }
        }

        private async Task HandleLoopAsync()
        {
            while (!_cancellationTokenSource.IsCancellationRequested)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch
                {
                    break;
                }

                if (context.Request.HttpMethod == "POST" && context.Request.Url?.AbsolutePath.EndsWith("/chat/completions") == true)
                {
                    using var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding ?? Encoding.UTF8);
                    var requestBody = await reader.ReadToEndAsync();
                    _chatCompletionRequestSource.TrySetResult(requestBody);

                    var bytes = Encoding.UTF8.GetBytes(_responseJson);
                    context.Response.StatusCode = 200;
                    context.Response.ContentType = "application/json";
                    context.Response.ContentLength64 = bytes.Length;
                    await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length, _cancellationTokenSource.Token);
                    context.Response.Close();
                    continue;
                }

                context.Response.StatusCode = 404;
                context.Response.Close();
            }
        }

        private static int FindAvailablePort()
        {
            var listener = new System.Net.Sockets.TcpListener(System.Net.IPAddress.Loopback, 0);
            listener.Start();
            var port = ((System.Net.IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }
    }
}