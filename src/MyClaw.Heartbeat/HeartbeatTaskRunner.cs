namespace MyClaw.Heartbeat;

/// <summary>
/// 心跳任务运行器 - 优先通过 AI CLI 自主执行，失败或不可用时回退到 OnHeartbeat 回调（Gateway/Agent）。
/// </summary>
public class HeartbeatTaskRunner
{
    private const string NoActionMarker = "HEARTBEAT_OK";

    private readonly AiCliDetector _detector = new();
    private readonly AutonomousExecutor _executor = new();

    /// <summary>
    /// 当无可用 AI CLI 或自主执行失败时，使用此回调（如 Gateway 的 Agent）
    /// </summary>
    public Func<string, Task<string>>? OnHeartbeat { get; set; }

    /// <summary>
    /// 执行一次心跳：先尝试 AI CLI 自主执行，否则调用 OnHeartbeat
    /// </summary>
    /// <param name="content">HEARTBEAT.md 的清单内容</param>
    /// <param name="ct">取消令牌</param>
    /// <returns>执行结果文本（用于日志/展示）</returns>
    public async Task<string> RunAsync(string content, CancellationToken ct = default)
    {
        var task = BuildTask(content);
        if (string.IsNullOrWhiteSpace(task.Prompt))
            return "";

        // 1. 尝试检测可用 AI CLI
        var clis = await _detector.DetectAvailableClisAsync(ct);
        if (clis.Count > 0)
        {
            // 2. 优先使用第一个可用的 CLI 自主执行
            var cli = clis[0];
            var result = await _executor.ExecuteAsync(task, cli, ct);
            if (result.Success)
            {
                if (result.NoActionNeeded)
                    return NoActionMarker;
                return result.Output ?? "";
            }
            // 自主执行失败，回退到 OnHeartbeat
        }

        // 3. 回退：通过 OnHeartbeat（Gateway/Agent）执行
        if (OnHeartbeat != null)
            return await OnHeartbeat(task.Prompt);

        return "";
    }

    /// <summary>
    /// 从 HEARTBEAT.md 内容构建 HeartbeatTask（含标准提示词）
    /// </summary>
    public static HeartbeatTask BuildTask(string content)
    {
        var prompt = $@"Heartbeat prompt: Check the following items for updates or actions needed.

{content.Trim()}

If nothing needs attention, reply exactly: HEARTBEAT_OK";
        return new HeartbeatTask
        {
            Content = content.Trim(),
            Prompt = prompt
        };
    }
}
