using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Status 命令 - 显示 myclaw 状态
/// </summary>
public class StatusCommand : Command
{
    public StatusCommand() : base("status", "显示 myclaw 状态")
    {
        this.SetHandler(() =>
        {
            MyClawConfiguration cfg;
            try
            {
                cfg = ConfigurationLoader.Load();
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]配置: 错误 ({ex.Message})[/]");
                return;
            }

            var table = new Table();
            table.AddColumn("属性");
            table.AddColumn("值");

            table.AddRow("配置文件", ConfigurationLoader.ConfigPath);
            table.AddRow("工作区", cfg.Agent.Workspace);
            table.AddRow("模型", cfg.Agent.Model);
            table.AddRow("提供商", string.IsNullOrEmpty(cfg.Provider.Type) ? "anthropic (默认)" : cfg.Provider.Type);

            // API 密钥（脱敏）及来源
            var (apiKeyDisplay, keySource) = GetApiKeyDisplay(cfg);
            table.AddRow("API 密钥", apiKeyDisplay);
            if (!string.IsNullOrEmpty(keySource))
            {
                table.AddRow("密钥来源", $"[blue]{keySource}[/]");
            }

            // 显示 Base URL（如果已设置）
            if (!string.IsNullOrEmpty(cfg.Provider.BaseUrl))
            {
                table.AddRow("Base URL", cfg.Provider.BaseUrl);
            }

            table.AddRow("Telegram", cfg.Channels.Telegram.Enabled ? "[green]已启用[/]" : "已禁用");
            table.AddRow("飞书", cfg.Channels.Feishu.Enabled ? "[green]已启用[/]" : "已禁用");
            table.AddRow("企业微信", cfg.Channels.WeCom.Enabled ? "[green]已启用[/]" : "已禁用");
            table.AddRow("Uno", cfg.Channels.Uno.Enabled ? $"[green]已启用[/] ({cfg.Channels.Uno.Mode})" : "已禁用");
            table.AddRow("技能", cfg.Skills.Enabled ? $"[green]已启用[/] (目录: {GetSkillsDir(cfg)})" : "已禁用");

            // 检查工作区
            if (!Directory.Exists(cfg.Agent.Workspace))
            {
                table.AddRow("工作区", "[red]未找到 (请运行 'myclaw onboard')[/]");
            }
            else
            {
                var memoryPath = Path.Combine(cfg.Agent.Workspace, "memory", "MEMORY.md");
                if (File.Exists(memoryPath))
                {
                    var content = File.ReadAllText(memoryPath);
                    table.AddRow("记忆", $"{content.Length} 字节");
                }
                else
                {
                    table.AddRow("记忆", "空");
                }
            }

            AnsiConsole.Write(table);
        });
    }

    private static string GetSkillsDir(MyClawConfiguration cfg)
    {
        return SkillPaths.ResolveSkillsDirectory(cfg.Agent.Workspace, cfg.Skills.Dir);
    }

    private static (string display, string source) GetApiKeyDisplay(MyClawConfiguration cfg)
    {
        if (string.IsNullOrEmpty(cfg.Provider.ApiKey))
        {
            return ("[red]未设置[/]", "");
        }

        // 从环境变量检测来源
        string source;
        if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OPENAI_API_KEY")))
            source = "环境变量: OPENAI_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY")))
            source = "环境变量: DEEPSEEK_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ANTHROPIC_API_KEY")))
            source = "环境变量: ANTHROPIC_API_KEY";
        else if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("MYCLAW_API_KEY")))
            source = "环境变量: MYCLAW_API_KEY";
        else
            source = "配置文件";

        // 脱敏密钥
        string display;
        if (cfg.Provider.ApiKey.Length > 8)
        {
            display = cfg.Provider.ApiKey[..4] + "..." + cfg.Provider.ApiKey[^4..];
        }
        else
        {
            display = "已设置";
        }

        return (display, source);
    }
}
