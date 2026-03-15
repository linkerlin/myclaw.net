namespace MyClaw.Core.Logging;

/// <summary>
/// 简单日志工具 - 所有输出到 stderr，避免干扰 stdout（用于 MCP stdio 兼容）
/// </summary>
public static class Log
{
    private static readonly object Lock = new();
    private static bool _useStderr = true; // 默认输出到 stderr

    /// <summary>
    /// 设置是否使用 stderr（否则使用 stdout）
    /// </summary>
    public static void UseStderr(bool useStderr)
    {
        _useStderr = useStderr;
    }

    /// <summary>
    /// 调试日志
    /// </summary>
    public static void Debug(string message)
    {
        WriteLine($"[DBG] {message}", ConsoleColor.Gray);
    }

    /// <summary>
    /// 信息日志
    /// </summary>
    public static void Info(string message)
    {
        WriteLine($"[INF] {message}", ConsoleColor.White);
    }

    /// <summary>
    /// 警告日志
    /// </summary>
    public static void Warn(string message)
    {
        WriteLine($"[WRN] {message}", ConsoleColor.Yellow);
    }

    /// <summary>
    /// 错误日志 - 始终输出到 stderr
    /// </summary>
    public static void Error(string message)
    {
        WriteLine($"[ERR] {message}", ConsoleColor.Red);
    }

    /// <summary>
    /// 带前缀的日志
    /// </summary>
    public static void Write(string prefix, string message)
    {
        WriteLine($"[{prefix}] {message}");
    }

    /// <summary>
    /// 原始输出（不带时间戳）
    /// </summary>
    private static void WriteLine(string message, ConsoleColor? color = null)
    {
        lock (Lock)
        {
            if (color.HasValue)
            {
                var originalColor = Console.ForegroundColor;
                Console.ForegroundColor = color.Value;
                
                if (_useStderr)
                    Console.Error.WriteLine(message);
                else
                    Console.WriteLine(message);
                
                Console.ForegroundColor = originalColor;
            }
            else
            {
                if (_useStderr)
                    Console.Error.WriteLine(message);
                else
                    Console.WriteLine(message);
            }
        }
    }
}
