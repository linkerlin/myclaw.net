using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Agent;
using MyClaw.Core.Configuration;
using MyClaw.Memory;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

public class AgentCommand : Command
{
    public AgentCommand() : base("agent", "在单消息或 REPL 模式下运行 Agent")
    {
        var messageOption = new Option<string?>(
            aliases: new[] { "-m", "--message" },
            description: "发送给 Agent 的单条消息");
            
        var modelOption = new Option<string>(
            aliases: new[] { "--model", "-M" },
            description: "指定使用的模型",
            getDefaultValue: () => "openai");
            
        var replOption = new Option<bool>(
            aliases: new[] { "--repl", "-r" },
            description: "强制使用 REPL 模式");

        AddOption(messageOption);
        AddOption(modelOption);
        AddOption(replOption);

        this.SetHandler(async (string? message, string model, bool repl) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            if (!string.IsNullOrEmpty(model))
            {
                cfg.Provider.Type = model;
            }
            
            if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
            {
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / OPENAI_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var memoryStore = new MemoryStore(cfg.Agent.Workspace);
            var skillManager = new SkillManager(cfg.Agent.Workspace);
            skillManager.LoadSkills();

            cfg.Provider.Model = cfg.Provider.Model ?? cfg.Agent.Model;
            
            // 使用新的 FallbackAgent
            var fallbackAgent = new FallbackAgent(cfg, memoryStore, skillManager);
            
            // 显示可用 Provider 列表
            AnsiConsole.MarkupLine("[dim]可用 Provider:[/]");
            foreach (var provider in fallbackAgent.AvailableProviders)
            {
                AnsiConsole.MarkupLine($"  - [cyan]{provider}[/]");
            }
            AnsiConsole.WriteLine();
            
            // 初始化（自动选择第一个可用的）
            await AnsiConsole.Status()
                .StartAsync("正在初始化模型...", async ctx =>
                {
                    await fallbackAgent.InitializeAsync();
                });
            
            AnsiConsole.MarkupLine($"[dim]当前 Provider:[/] [green]{fallbackAgent.CurrentProvider}[/]");
            AnsiConsole.WriteLine();

            if (!string.IsNullOrEmpty(message) && !repl)
            {
                await RunSingleMessageAsync(fallbackAgent, message);
            }
            else
            {
                await RunReplAsync(fallbackAgent);
            }
        }, messageOption, modelOption, replOption);
    }

    private async Task RunSingleMessageAsync(FallbackAgent agent, string message)
    {
        string response = "";
        await AnsiConsole.Status()
            .StartAsync("思考中...", async ctx =>
            {
                response = await agent.ChatAsync(message);
            });

        AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
    }

    private async Task RunReplAsync(FallbackAgent agent)
    {
        AnsiConsole.MarkupLine("[blue]myclaw agent (输入 'exit' 或 '/quit' 退出)[/]");
        
        while (true)
        {
            Console.Write("> ");
            var input = Console.ReadLine();
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() is "exit" or "quit" or "/quit")
                break;

            // 特殊命令：查看当前 provider
            if (input.ToLower() is "/provider" or "/status")
            {
                AnsiConsole.MarkupLine($"[dim]当前 Provider:[/] [cyan]{agent.CurrentProvider}[/]");
                continue;
            }

            string response = "";
            string provider = "";
            
            await AnsiConsole.Status()
                .StartAsync("思考中...", async ctx =>
                {
                    provider = agent.CurrentProvider;
                    response = await agent.ChatAsync(input);
                });

            // 如果降级了，显示提示
            if (provider != agent.CurrentProvider)
            {
                AnsiConsole.MarkupLine($"[yellow]⚠️ 已自动降级到: {agent.CurrentProvider}[/]");
            }

            AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
        }
    }
}
