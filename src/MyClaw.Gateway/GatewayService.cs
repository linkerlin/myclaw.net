using MyClaw.Agent;
using MyClaw.Channels;
using MyClaw.Core.Analytics;
using MyClaw.Core.Configuration;
using MyClaw.Core.Curiosity;
using MyClaw.Core.Messaging;
using MyClaw.Core.Mycelium;
using MyClaw.Cron;
using MyClaw.Heartbeat;
using MyClaw.Memory;
using MyClaw.Skills;

namespace MyClaw.Gateway;

#pragma warning disable CS0618 // MessageBus 兼容性别名

/// <summary>
/// Gateway 服务 - 协调所有组件（渠道、定时任务、心跳）
/// </summary>
public class GatewayService
{
    private readonly MyClawConfiguration _config;
    private readonly MessageBus _messageBus;
    private readonly ChannelManager _channelManager;
    private readonly MemoryStore _memoryStore;
    private readonly SkillManager _skillManager;
    private readonly CronService _cronService;
    private readonly HeartbeatService _heartbeatService;
    private readonly AnalyticsService _analyticsService;
    private readonly BoredomEngine _boredomEngine;
    private readonly MyceliumNetwork _myceliumNetwork;
    private MyClawAgent? _agent;
    private CancellationTokenSource? _cts;

    public GatewayService(MyClawConfiguration config)
    {
        _config = config;
        _messageBus = new MessageBus(MyClawConfiguration.DefaultBufSize);
        _channelManager = new ChannelManager(_messageBus);
        _memoryStore = new MemoryStore(config.Agent.Workspace);
        _skillManager = new SkillManager(SkillPaths.ResolveSkillsDirectory(config.Agent.Workspace, config.Skills.Dir));
        _analyticsService = new AnalyticsService(config.Agent.Workspace);
        _myceliumNetwork = new MyceliumNetwork(config.Agent.Workspace);
        _boredomEngine = new BoredomEngine(
            config.Agent.Workspace,
            config.Agent.Workspace,
            () => Task.FromResult(_analyticsService.GetAnalytics()),
            analytics =>
            {
                if (DateTime.TryParse(analytics.LastBoredomExecution, out var executedAt))
                {
                    _analyticsService.TrackBoredomExecution(executedAt);
                }

                return Task.CompletedTask;
            });
        
        // 加载 Skills
        _skillManager.LoadSkills();
        
        // Cron 服务
        var cronStorePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".myclaw", "data", "cron", "jobs.json");
        _cronService = new CronService(cronStorePath);
        _cronService.OnJob = ExecuteCronJobAsync;
        
        // Heartbeat 服务
        _heartbeatService = new HeartbeatService(config.Agent.Workspace);
        _heartbeatService.OnHeartbeat = ExecuteHeartbeatAsync;
        _heartbeatService.BuildSupplementalContext = BuildHeartbeatSupplementalContextAsync;
    }

    /// <summary>
    /// 启动 Gateway
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;
        var bootStart = DateTime.UtcNow;

        try
        {
            // 检查 API Key
            if (string.IsNullOrEmpty(_config.Provider.ApiKey))
            {
                throw new InvalidOperationException(
                    "API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY 环境变量。");
            }

            // 初始化 Agent
            _config.Provider.Model = _config.Provider.Model ?? _config.Agent.Model;
            var model = ModelFactory.Create(_config.Provider);
            _agent = new MyClawAgent(_config, model, _memoryStore, _skillManager);
            Console.WriteLine("[gateway] Agent 已初始化");

            // 初始化渠道
            _channelManager.InitializeChannels(_config.Channels);
            
            // 启动出站消息分发
            _ = Task.Run(() => _messageBus.DispatchOutboundAsync(token), token);

            // 启动所有渠道
            await _channelManager.StartAllAsync(token);
            Console.WriteLine($"[gateway] 渠道已启动: {string.Join(", ", _channelManager.GetEnabledChannels())}");

            // 启动 Cron 服务
            await _cronService.StartAsync(token);

            // 启动 Heartbeat 服务
            _ = Task.Run(() => _heartbeatService.StartAsync(token), token);

            _analyticsService.TrackBoot((long)(DateTime.UtcNow - bootStart).TotalMilliseconds);

            // 启动消息处理循环
            await ProcessLoopAsync(token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[gateway] 操作已取消");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[gateway] 错误: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// 停止 Gateway
    /// </summary>
    public async Task StopAsync()
    {
        Console.WriteLine("[gateway] 正在关闭...");
        
        _cts?.Cancel();
        
        _heartbeatService.Stop();
        await _cronService.StopAsync();
        await _channelManager.StopAllAsync();
        _messageBus.Close();
        
        Console.WriteLine("[gateway] 关闭完成");
    }

    /// <summary>
    /// 执行 Cron 任务
    /// </summary>
    private async Task<string> ExecuteCronJobAsync(CronJob job)
    {
        // 执行 Agent 处理
        var result = await ProcessMessageAsync(job.Payload.Message, "cron");
        
        // 如果需要投递到渠道
        if (job.Payload.Deliver && !string.IsNullOrEmpty(job.Payload.Channel))
        {
            await _messageBus.PublishOutboundAsync(new OutboundMessage
            {
                Channel = job.Payload.Channel,
                ChatID = job.Payload.To,
                Content = result
            });
        }
        
        return result;
    }

    /// <summary>
    /// 执行 Heartbeat 任务
    /// </summary>
    private async Task<string> ExecuteHeartbeatAsync(string prompt)
    {
        return await ProcessMessageAsync(prompt, "heartbeat");
    }

    /// <summary>
    /// 消息处理循环
    /// </summary>
    private async Task ProcessLoopAsync(CancellationToken ct)
    {
        await foreach (var message in _messageBus.InboundReader.ReadAllAsync(ct))
        {
            try
            {
                Console.WriteLine($"[gateway] inbound from {message.Channel}/{message.SenderID}: {Truncate(message.Content, 80)}");

                var response = await ProcessMessageAsync(message.Content, message.SessionKey);

                if (!string.IsNullOrEmpty(response))
                {
                    await _messageBus.PublishOutboundAsync(new OutboundMessage
                    {
                        Channel = message.Channel,
                        ChatID = message.ChatID,
                        Content = response
                    }, ct);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[gateway] 消息处理错误: {ex.Message}");
                
                // 发送错误回复
                await _messageBus.PublishOutboundAsync(new OutboundMessage
                {
                    Channel = message.Channel,
                    ChatID = message.ChatID,
                    Content = "抱歉，处理您的消息时遇到错误。"
                }, ct);
            }
        }
    }

    /// <summary>
    /// 处理消息（调用 Agent）
    /// </summary>
    private async Task<string> ProcessMessageAsync(string prompt, string sessionId)
    {
        if (_agent == null)
        {
            return "错误: Agent 未初始化";
        }

        if (!string.Equals(sessionId, "heartbeat", StringComparison.OrdinalIgnoreCase))
        {
            var promptName = string.Equals(sessionId, "cron", StringComparison.OrdinalIgnoreCase)
                ? "cron"
                : "chat";
            _analyticsService.TrackPrompt(promptName);
        }

        return await _agent.ChatAsync(prompt, sessionId);
    }

    private async Task<string?> BuildHeartbeatSupplementalContextAsync()
    {
        var sections = new List<string>();
        var verboseSupplementalContext = _config.Agent.Verbose;

        var hookSection = _skillManager.BuildHookContext(SkillHookType.Heartbeat);
        if (!string.IsNullOrWhiteSpace(hookSection))
        {
            sections.Add(hookSection);
        }

        var absorption = await _myceliumNetwork.AbsorbSporesAsync(_config.Agent.Workspace);
        var myceliumSection = HeartbeatSupplementalContextFormatter.FormatMycelium(absorption, verboseSupplementalContext);
        if (!string.IsNullOrWhiteSpace(myceliumSection))
        {
            sections.Add(myceliumSection);
        }

        var boredom = await _boredomEngine.CheckAndExecuteAsync();
        var boredomSection = HeartbeatSupplementalContextFormatter.FormatBoredom(
            boredom,
            _config.Agent.Workspace,
            verboseSupplementalContext);
        if (!string.IsNullOrWhiteSpace(boredomSection))
        {
            sections.Add(boredomSection);
        }

        return sections.Count == 0 ? null : string.Join("\n\n", sections);
    }

    private static string Truncate(string s, int n)
    {
        if (s.Length <= n)
            return s;
        return s[..n] + "...";
    }
}

#pragma warning restore CS0618
