namespace MyClaw.Heartbeat;

/// <summary>
/// 心跳服务 - 周期性执行任务。优先通过 AI CLI 自主执行，无 CLI 时使用 OnHeartbeat 回调。
/// </summary>
public class HeartbeatService
{
    private readonly string _workspace;
    private readonly TimeSpan _interval;
    private readonly HeartbeatTaskRunner _runner;
    private CancellationTokenSource? _cts;

    /// <summary>
    /// 心跳回调（无可用 AI CLI 或自主执行失败时使用，如 Gateway 的 Agent）
    /// </summary>
    public Func<string, Task<string>>? OnHeartbeat
    {
        get => _runner.OnHeartbeat;
        set => _runner.OnHeartbeat = value;
    }

    /// <summary>
    /// 每次心跳前构建附加上下文，可带有副作用（如吸收孢子、无聊扫描）。
    /// </summary>
    public Func<Task<string?>>? BuildSupplementalContext { get; set; }

    public HeartbeatService(string workspace, TimeSpan? interval = null, HeartbeatTaskRunner? runner = null)
    {
        _workspace = workspace;
        _interval = interval ?? TimeSpan.FromMinutes(30);
        _runner = runner ?? new HeartbeatTaskRunner();
    }

    /// <summary>
    /// 启动心跳服务
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var token = _cts.Token;

        Console.WriteLine($"[heartbeat] 已启动, 间隔={_interval}");

        using var timer = new PeriodicTimer(_interval);

        try
        {
            while (await timer.WaitForNextTickAsync(token))
            {
                await RunOnceAsync();
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("[heartbeat] 已停止");
        }
    }

    /// <summary>
    /// 停止心跳服务
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 执行一次心跳
    /// </summary>
    public async Task RunOnceAsync()
    {
        var hbPath = Path.Combine(_workspace, "HEARTBEAT.md");
        
        if (!File.Exists(hbPath))
        {
            return;
        }

        try
        {
            var content = await File.ReadAllTextAsync(hbPath);
            content = content.Trim();
            var supplementalContext = string.Empty;
            var supplementalContextBuilder = BuildSupplementalContext;
            if (supplementalContextBuilder != null)
            {
                supplementalContext = (await supplementalContextBuilder.Invoke() ?? string.Empty).Trim();
            }
            var hasBaseWork = !string.IsNullOrEmpty(content) &&
                !(content.StartsWith("# HEARTBEAT.md") && !content.Contains("- "));
            var hasSupplementalWork = !string.IsNullOrWhiteSpace(supplementalContext);

            if (!hasBaseWork && !hasSupplementalWork)
            {
                return;
            }

            var effectiveContent = hasBaseWork ? content : string.Empty;
            if (hasSupplementalWork)
            {
                effectiveContent = string.IsNullOrWhiteSpace(effectiveContent)
                    ? supplementalContext
                    : $"{effectiveContent}\n\n{supplementalContext}";
            }

            Console.WriteLine($"[heartbeat] 触发心跳，提示词 ({effectiveContent.Length} 字符)");

            var result = await _runner.RunAsync(effectiveContent);

            if (string.IsNullOrEmpty(result))
            {
                if (_runner.OnHeartbeat == null)
                    Console.WriteLine("[heartbeat] 未设置处理程序且无可用 AI CLI");
            }
            else if (result.Contains("HEARTBEAT_OK"))
            {
                Console.WriteLine("[heartbeat] 无需执行");
            }
            else
            {
                Console.WriteLine($"[heartbeat] 结果: {Truncate(result, 200)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[heartbeat] 错误: {ex.Message}");
        }
    }

    private static string Truncate(string s, int n)
    {
        if (s.Length <= n)
            return s;
        return s[..n] + "...";
    }
}
