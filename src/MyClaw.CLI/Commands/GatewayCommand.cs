using System;
using System.CommandLine;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using MyClaw.Gateway;
using MyClaw.MCP;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Gateway 命令 - 启动完整网关服务（包含MCP服务）
/// </summary>
public class GatewayCommand : Command
{
    private static void KillProcessOnPort(int port)
    {
        var properties = IPGlobalProperties.GetIPGlobalProperties();
        var listeners = properties.GetActiveTcpListeners();
        
        foreach (var listener in listeners)
        {
            if (listener.Port == port)
            {
                AnsiConsole.MarkupLine($"[yellow]端口 {port} 已被占用，正在终止占用进程...[/]");
                
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "netstat",
                        Arguments = $"-ano | findstr :{port}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    
                    using var process = Process.Start(psi);
                    if (process == null) return;
                    
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    
                    var lines = output.Split('\n');
                    foreach (var line in lines)
                    {
                        var parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 5 && parts[0] == "TCP")
                        {
                            if (int.TryParse(parts[4], out var pid) && pid > 0)
                            {
                                try
                                {
                                    var killProcess = Process.GetProcessById(pid);
                                    var processName = killProcess.ProcessName;
                                    AnsiConsole.MarkupLine($"[yellow]终止进程: {processName} (PID: {pid})[/]");
                                    killProcess.Kill();
                                    killProcess.WaitForExit(3000);
                                    AnsiConsole.MarkupLine($"[green]✓ 进程已终止[/]");
                                }
                                catch (Exception ex)
                                {
                                    AnsiConsole.MarkupLine($"[red]无法终止进程 PID {pid}: {ex.Message}[/]");
                                }
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]检查端口失败: {ex.Message}[/]");
                }
                break;
            }
        }
    }
    
    public GatewayCommand() : base("gateway", "启动完整网关（渠道 + 定时任务 + 心跳 + MCP）")
    {
        // MCP 端口参数，默认 2334
        var mcpPortOption = new Option<int>(
            aliases: new[] { "--mcpport" },
            description: "MCP 服务端口（默认: 2334）",
            getDefaultValue: () => 2334);

        AddOption(mcpPortOption);

        this.SetHandler(async (int mcpPort) =>
        {
            var cfg = ConfigurationLoader.Load();

            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var cts = new CancellationTokenSource();
            
            // 处理 Ctrl+C
            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            // MCP 服务现在使用 stdio 模式，需要作为子进程启动
            AnsiConsole.MarkupLine($"[blue]注意: MCP 服务已改为 stdio 模式，请在单独的终端运行 'myclaw mcp'[/]");

            // 检查并清理网关端口
            KillProcessOnPort(cfg.Gateway.Port);
            
            // 检查并清理 WebUI 端口
            var webuiPort = cfg.Channels?.WebUI?.Port ?? 8080;
            if (cfg.Channels?.WebUI?.Enabled == true)
            {
                KillProcessOnPort(webuiPort);
            }

            // 启动网关
            AnsiConsole.MarkupLine($"[blue]正在 {cfg.Gateway.Host}:{cfg.Gateway.Port} 启动网关...[/]");
            
            try
            {
                var gateway = new GatewayService(cfg);
                _ = Task.Run(() => gateway.StartAsync(cts.Token), cts.Token);
                
                AnsiConsole.MarkupLine($"[green]✓ 网关已启动 {cfg.Gateway.Host}:{cfg.Gateway.Port}[/]");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[blue]运行中的服务:[/]");
                AnsiConsole.MarkupLine($"  • MCP 服务器:   http://localhost:{mcpPort}/mcp");
                AnsiConsole.MarkupLine($"  • 网关:         {cfg.Gateway.Host}:{cfg.Gateway.Port}");
                AnsiConsole.MarkupLine($"  • WebUI:        http://localhost:{cfg.Channels?.WebUI?.Port ?? 8080} (如已启用)");
                AnsiConsole.MarkupLine("");
                AnsiConsole.MarkupLine("[yellow]按 Ctrl+C 停止所有服务[/]");
                
                // 等待关闭信号
                await Task.Delay(-1, cts.Token);
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("\n[yellow]正在关闭服务...[/]");
                AnsiConsole.MarkupLine("[green]✓ 服务已停止[/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]网关错误: {ex.Message}[/]");
            }
        }, mcpPortOption);
    }
}
