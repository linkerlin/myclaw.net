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
            getDefaultValue: () => "anthropic");
            
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
                AnsiConsole.MarkupLine("[red]API 密钥未设置。请运行 'myclaw onboard' 或设置 MYCLAW_API_KEY / ANTHROPIC_API_KEY[/]");
                return;
            }

            var memoryStore = new MemoryStore(cfg.Agent.Workspace);
            var skillManager = new SkillManager(cfg.Agent.Workspace);
            skillManager.LoadSkills();

            cfg.Provider.Model = cfg.Provider.Model ?? cfg.Agent.Model;
            
            // 显示当前使用的 LLM 配置
            AnsiConsole.MarkupLine($"[dim]Provider:[/] [cyan]{cfg.Provider.Type}[/]");
            AnsiConsole.MarkupLine($"[dim]Model:[/] [cyan]{cfg.Provider.Model}[/]");
            if (!string.IsNullOrEmpty(cfg.Provider.BaseUrl))
            {
                AnsiConsole.MarkupLine($"[dim]BaseUrl:[/] [cyan]{cfg.Provider.BaseUrl}[/]");
            }
            AnsiConsole.MarkupLine($"[dim]API Key:[/] [cyan]***{cfg.Provider.ApiKey[^4..]}[/]");
            AnsiConsole.WriteLine();
            
            var modelInstance = ModelFactory.Create(cfg.Provider);
            var agent = new MyClawAgent(cfg, modelInstance, memoryStore, skillManager);

            if (!string.IsNullOrEmpty(message) && !repl)
            {
                await RunSingleMessageAsync(agent, message);
            }
            else
            {
                await RunReplAsync(agent);
            }
        }, messageOption, modelOption, replOption);
    }

    private async Task RunSingleMessageAsync(MyClawAgent agent, string message)
    {
        string response = "";
        await AnsiConsole.Status()
            .StartAsync("思考中...", async ctx =>
            {
                response = await agent.ChatAsync(message);
            });

        AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
    }

    private async Task RunReplAsync(MyClawAgent agent)
    {
        AnsiConsole.MarkupLine("[blue]myclaw agent (输入 'exit' 或 '/quit' 退出)[/]");
        
        while (true)
        {
            var input = AnsiConsole.Ask<string?>("> ");
            
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.ToLower() is "exit" or "quit" or "/quit")
                break;

            string response = "";
            await AnsiConsole.Status()
                .StartAsync("思考中...", async ctx =>
                {
                    response = await agent.ChatAsync(input);
                });

            AnsiConsole.MarkupLine($"[green]助手:[/] {response}");
        }
    }
}
