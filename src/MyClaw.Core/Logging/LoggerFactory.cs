using Microsoft.Extensions.Logging;
using Serilog;

namespace MyClaw.Core.Logging;

/// <summary>
/// 日志工厂 - 创建和配置日志记录器
/// </summary>
public static class LoggerFactory
{
    private static Microsoft.Extensions.Logging.ILoggerFactory? _factory;
    private static readonly object _lock = new();

    /// <summary>
    /// 获取或创建日志工厂
    /// </summary>
    public static Microsoft.Extensions.Logging.ILoggerFactory GetFactory()
    {
        if (_factory == null)
        {
            lock (_lock)
            {
                _factory ??= CreateFactory();
            }
        }
        return _factory;
    }

    /// <summary>
    /// 创建指定类别的日志记录器
    /// </summary>
    public static ILogger<T> CreateLogger<T>()
    {
        return GetFactory().CreateLogger<T>();
    }

    /// <summary>
    /// 创建指定类别的日志记录器
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger CreateLogger(string categoryName)
    {
        return GetFactory().CreateLogger(categoryName);
    }

    /// <summary>
    /// 创建日志工厂
    /// </summary>
    private static Microsoft.Extensions.Logging.ILoggerFactory CreateFactory()
    {
        // 配置 Serilog - 使用 stderr 输出以避免干扰 stdout（MCP stdio 兼容）
        Serilog.Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                standardErrorFromLevel: Serilog.Events.LogEventLevel.Debug)  // 所有日志输出到 stderr
            .CreateLogger();

        return new Microsoft.Extensions.Logging.LoggerFactory()
            .AddSerilog();
    }

    /// <summary>
    /// 关闭并刷新日志
    /// </summary>
    public static void Shutdown()
    {
        Serilog.Log.CloseAndFlush();
    }
}
