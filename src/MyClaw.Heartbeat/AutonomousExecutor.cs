namespace MyClaw.Heartbeat;

/// <summary>
/// 自主执行结果
/// </summary>
public sealed class ExecutionResult
{
    public bool Success { get; set; }
    public bool NoActionNeeded { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }

    public static ExecutionResult NoActionNeededResult()
        => new() { Success = true, NoActionNeeded = true };

    public static ExecutionResult SuccessWithOutput(string output)
        => new() { Success = true, NoActionNeeded = false, Output = output };

    public static ExecutionResult Failed(string? error = null)
        => new() { Success = false, Error = error };
}

/// <summary>
/// 自主执行器 - 通过已检测的 AI CLI 执行 Heartbeat 提示，无需 Gateway/Agent 在线。
/// </summary>
public class AutonomousExecutor
{
    private const string NoActionMarker = "HEARTBEAT_OK";
    private static readonly TimeSpan CliTimeout = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 使用指定 AI CLI 执行心跳任务
    /// </summary>
    /// <param name="task">心跳任务（含组装好的 Prompt）</param>
    /// <param name="cli">已检测可用的 CLI 信息</param>
    /// <param name="ct">取消令牌</param>
    public async Task<ExecutionResult> ExecuteAsync(HeartbeatTask task, AiCliInfo cli, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(task.Prompt))
            return ExecutionResult.Failed("Prompt 为空");

        try
        {
            var result = await ExecuteCliAsync(cli, task.Prompt, ct);
            if (result.Contains(NoActionMarker, StringComparison.OrdinalIgnoreCase))
                return ExecutionResult.NoActionNeededResult();
            return ExecutionResult.SuccessWithOutput(result);
        }
        catch (Exception ex)
        {
            return ExecutionResult.Failed(ex.Message);
        }
    }

    /// <summary>
    /// 调用 AI CLI：将 prompt 通过标准输入传入，读取标准输出
    /// </summary>
    public async Task<string> ExecuteCliAsync(AiCliInfo cli, string prompt, CancellationToken ct = default)
    {
        var exe = cli.Path ?? cli.ExecutableName;
        // 多数 AI CLI 无参数时从 stdin 读取提示；若有需可在此按 cli.Name 分支
        var arguments = GetRunArguments(cli);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(CliTimeout);

        var (exitCode, stdout) = await AiCliDetector.RunCommandAsync(exe, arguments, prompt, cts.Token);
        if (exitCode != 0)
            throw new InvalidOperationException($"CLI 退出码: {exitCode}");
        return stdout.Trim();
    }

    /// <summary>
    /// 各 CLI 运行时的参数（空表示仅从 stdin 读）
    /// </summary>
    private static string GetRunArguments(AiCliInfo cli)
    {
        // 多数 AI CLI 无参数时从 stdin 读取提示
        return "";
    }
}
