namespace MyClaw.Heartbeat;

/// <summary>
/// 检测到的 AI CLI 信息
/// </summary>
public sealed class AiCliInfo
{
    public string Name { get; set; } = "";
    public string? Path { get; set; }
    public string? Version { get; set; }
    /// <summary>用于检测是否可用的命令，如 claude --version</summary>
    public string VersionCommand { get; set; } = "";
    /// <summary>运行时的可执行文件名（用于 which/where 查找）</summary>
    public string ExecutableName { get; set; } = "";
}

/// <summary>
/// AI CLI 检测器 - 检测系统上可用的 AI 命令行工具（claude/gemini/kimi/aider）
/// 用于 Heartbeat 自主执行：当检测到可用 CLI 时，可通过其执行心跳任务而无需 Gateway/Agent 在线。
/// </summary>
public class AiCliDetector
{
    private static readonly List<AiCliInfo> KnownClis = new()
    {
        new AiCliInfo
        {
            Name = "claude",
            ExecutableName = "claude",
            VersionCommand = "claude --version"
        },
        new AiCliInfo
        {
            Name = "gemini",
            ExecutableName = "gemini",
            VersionCommand = "gemini --version"
        },
        new AiCliInfo
        {
            Name = "kimi",
            ExecutableName = "kimi",
            VersionCommand = "kimi --version"
        },
        new AiCliInfo
        {
            Name = "aider",
            ExecutableName = "aider",
            VersionCommand = "aider --version"
        }
    };

    /// <summary>
    /// 检测所有已知 AI CLI 中可用的实例
    /// </summary>
    public async Task<List<AiCliInfo>> DetectAvailableClisAsync(CancellationToken ct = default)
    {
        var available = new List<AiCliInfo>();

        foreach (var cli in KnownClis)
        {
            try
            {
                if (await IsAvailableAsync(cli, ct))
                {
                    var path = await GetPathAsync(cli.ExecutableName, ct);
                    var version = await GetVersionAsync(cli, ct);
                    available.Add(new AiCliInfo
                    {
                        Name = cli.Name,
                        Path = path,
                        Version = version,
                        VersionCommand = cli.VersionCommand,
                        ExecutableName = cli.ExecutableName
                    });
                }
            }
            catch
            {
                // 忽略单个 CLI 检测失败
            }
        }

        return available;
    }

    /// <summary>
    /// 检查指定 CLI 是否可用（能成功执行 version 命令）
    /// </summary>
    public static async Task<bool> IsAvailableAsync(AiCliInfo cli, CancellationToken ct = default)
    {
        try
        {
            var args = GetVersionArgs(cli);
            var (exitCode, _) = await RunCommandAsync(cli.ExecutableName, args, null, ct);
            return exitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 获取可执行文件路径（where.exe / which）
    /// </summary>
    public static async Task<string?> GetPathAsync(string executableName, CancellationToken ct = default)
    {
        var isWindows = OperatingSystem.IsWindows();
        var (cmd, args) = isWindows
            ? ("where.exe", executableName)
            : ("which", executableName);

        var (exitCode, output) = await RunCommandAsync(cmd, args, null, ct);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        var firstLine = output.Trim().Split('\n', '\r')[0].Trim();
        return string.IsNullOrEmpty(firstLine) ? null : firstLine;
    }

    /// <summary>
    /// 获取 CLI 版本字符串
    /// </summary>
    public static async Task<string?> GetVersionAsync(AiCliInfo cli, CancellationToken ct = default)
    {
        var args = GetVersionArgs(cli);
        var (exitCode, output) = await RunCommandAsync(cli.ExecutableName, args, null, ct);
        if (exitCode != 0 || string.IsNullOrWhiteSpace(output))
            return null;

        var firstLine = output.Trim().Split('\n', '\r')[0].Trim();
        return string.IsNullOrEmpty(firstLine) ? null : firstLine;
    }

    private static string GetVersionArgs(AiCliInfo cli)
    {
        var cmd = cli.VersionCommand;
        var parts = cmd.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length > 1 ? parts[1] : "--version";
    }

    /// <summary>
    /// 执行外部命令，返回退出码和标准输出
    /// </summary>
    internal static async Task<(int ExitCode, string StdOut)> RunCommandAsync(
        string fileName,
        string arguments,
        string? stdin,
        CancellationToken ct = default)
    {
        var startInfo = new System.Diagnostics.ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = stdin != null,
            CreateNoWindow = true
        };

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process == null)
            return (-1, "");

        if (stdin != null)
        {
            await process.StandardInput.WriteAsync(stdin);
            process.StandardInput.Close();
        }

        var stdout = await process.StandardOutput.ReadToEndAsync(ct);
        await process.WaitForExitAsync(ct);
        return (process.ExitCode, stdout);
    }
}
