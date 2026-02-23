using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// SyncTemplates 命令 - 同步模板文件到工作区
/// </summary>
public class SyncTemplatesCommand : Command
{
    public SyncTemplatesCommand() : base("sync-templates", "同步模板文件到工作区")
    {
        // 添加选项
        var forceOption = new Option<bool>(
            aliases: new[] { "--force", "-f" },
            description: "强制覆盖已存在的文件");
        
        var dryRunOption = new Option<bool>(
            aliases: new[] { "--dry-run", "-n" },
            description: "仅显示将要执行的操作，不实际复制");

        AddOption(forceOption);
        AddOption(dryRunOption);

        this.SetHandler((bool force, bool dryRun) =>
        {
            ExecuteSync(force, dryRun);
        }, forceOption, dryRunOption);
    }

    private static void ExecuteSync(bool force, bool dryRun)
    {
        // 加载配置
        var cfg = ConfigurationLoader.Load();
        var workspacePath = cfg.Agent.Workspace;

        AnsiConsole.MarkupLine($"[blue]工作区: {workspacePath}[/]");

        // 确保工作区存在
        if (!Directory.Exists(workspacePath))
        {
            if (!dryRun)
            {
                Directory.CreateDirectory(workspacePath);
                AnsiConsole.MarkupLine($"[green]创建工作区: {workspacePath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[dim]将要创建: {workspacePath}[/]");
            }
        }

        // 查找 templates 目录
        var templatesDir = FindTemplatesDirectory();
        
        if (string.IsNullOrEmpty(templatesDir))
        {
            AnsiConsole.MarkupLine("[red]错误: 未找到 templates 目录[/]");
            AnsiConsole.MarkupLine("[dim]尝试设置 MYCLAW_TEMPLATES_DIR 环境变量指向 templates 目录[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[blue]模板源: {templatesDir}[/]");
        AnsiConsole.WriteLine();

        // 获取所有 .md 文件
        var templateFiles = Directory.GetFiles(templatesDir, "*.md")
            .Select(Path.GetFileName)
            .Where(f => f != null)
            .ToList();

        if (templateFiles.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]警告: templates 目录中没有 .md 文件[/]");
            return;
        }

        // 显示表格
        var table = new Table();
        table.AddColumn("文件");
        table.AddColumn("状态");
        table.AddColumn("操作");

        var copiedCount = 0;
        var skippedCount = 0;
        var wouldCopyCount = 0;

        foreach (var fileName in templateFiles!)
        {
            var sourcePath = Path.Combine(templatesDir, fileName!);
            var targetPath = Path.Combine(workspacePath, fileName!);
            var exists = File.Exists(targetPath);

            string status;
            string action;

            if (exists)
            {
                if (force)
                {
                    status = "[yellow]已存在[/]";
                    action = dryRun ? "[dim]将覆盖[/]" : "[yellow]覆盖[/]";
                    if (!dryRun)
                    {
                        try
                        {
                            File.Copy(sourcePath, targetPath, overwrite: true);
                            copiedCount++;
                        }
                        catch (Exception ex)
                        {
                            action = $"[red]失败: {ex.Message}[/]";
                        }
                    }
                    else
                    {
                        wouldCopyCount++;
                    }
                }
                else
                {
                    status = "[dim]已存在[/]";
                    action = "[dim]跳过[/]";
                    skippedCount++;
                }
            }
            else
            {
                status = "[green]新文件[/]";
                action = dryRun ? "[dim]将复制[/]" : "[green]复制[/]";
                if (!dryRun)
                {
                    try
                    {
                        File.Copy(sourcePath, targetPath);
                        copiedCount++;
                    }
                    catch (Exception ex)
                    {
                        action = $"[red]失败: {ex.Message}[/]";
                    }
                }
                else
                {
                    wouldCopyCount++;
                }
            }

            table.AddRow(fileName!, status, action);
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();

        if (dryRun)
        {
            AnsiConsole.MarkupLine($"[blue]预览模式: 将复制 {wouldCopyCount} 个文件[/]");
            AnsiConsole.MarkupLine("[dim]使用 --force 强制覆盖已存在的文件[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]完成: {copiedCount} 个文件已处理, {skippedCount} 个跳过[/]");
        }
    }

    /// <summary>
    /// 查找项目中的 templates 目录
    /// </summary>
    private static string? FindTemplatesDirectory()
    {
        // 1. 首先检查环境变量
        var envTemplates = Environment.GetEnvironmentVariable("MYCLAW_TEMPLATES_DIR");
        if (!string.IsNullOrEmpty(envTemplates) && Directory.Exists(envTemplates))
        {
            return envTemplates;
        }

        // 2. 从当前工作目录向上查找
        var currentDir = Directory.GetCurrentDirectory();
        var searchDir = currentDir;
        
        for (int i = 0; i < 5; i++) // 向上查找最多 5 层
        {
            var templatesPath = Path.Combine(searchDir, "templates");
            if (Directory.Exists(templatesPath))
            {
                // 验证是否包含核心模板文件
                if (File.Exists(Path.Combine(templatesPath, "AGENTS.md")) ||
                    File.Exists(Path.Combine(templatesPath, "SOUL.md")))
                {
                    return templatesPath;
                }
            }

            var parentDir = Directory.GetParent(searchDir);
            if (parentDir == null) break;
            searchDir = parentDir.FullName;
        }

        // 3. 检查可执行文件所在目录
        var exeDir = AppContext.BaseDirectory;
        var exeTemplatesPath = Path.Combine(exeDir, "templates");
        if (Directory.Exists(exeTemplatesPath))
        {
            return exeTemplatesPath;
        }

        // 4. 检查用户配置目录
        var userTemplatesPath = Path.Combine(ConfigurationLoader.ConfigDir, "templates");
        if (Directory.Exists(userTemplatesPath))
        {
            return userTemplatesPath;
        }

        return null;
    }
}
