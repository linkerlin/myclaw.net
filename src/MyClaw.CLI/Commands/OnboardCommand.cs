using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Onboard 命令 - 初始化配置和工作区，从 templates 复制模板文件
/// </summary>
public class OnboardCommand : Command
{
    public OnboardCommand() : base("onboard", "初始化配置和工作区")
    {
        this.SetHandler(() =>
        {
            var cfgDir = ConfigurationLoader.ConfigDir;
            var cfgPath = ConfigurationLoader.ConfigPath;

            Directory.CreateDirectory(cfgDir);

            // 如果配置文件不存在则创建默认配置
            if (!File.Exists(cfgPath))
            {
                var defaultConfig = MyClawConfiguration.Default();
                ConfigurationLoader.Save(defaultConfig);
                AnsiConsole.MarkupLine($"[green]已创建配置: {cfgPath}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[yellow]配置已存在: {cfgPath}[/]");
            }

            // 加载配置以获取工作区
            var cfg = ConfigurationLoader.Load();
            var ws = cfg.Agent.Workspace;

            // 创建工作区目录
            Directory.CreateDirectory(ws);
            AnsiConsole.MarkupLine($"[green]已创建工作区: {ws}[/]");

            // 创建子目录
            var memoryDir = Path.Combine(ws, "memory");
            Directory.CreateDirectory(memoryDir);
            AnsiConsole.MarkupLine($"[green]已创建记忆目录: {memoryDir}[/]");

            var skillsDir = string.IsNullOrEmpty(cfg.Skills.Dir) 
                ? Path.Combine(ws, "skills") 
                : cfg.Skills.Dir;
            Directory.CreateDirectory(skillsDir);
            AnsiConsole.MarkupLine($"[green]已创建技能目录: {skillsDir}[/]");

            // 从项目 templates 目录复制模板文件
            SyncTemplates(ws);

            AnsiConsole.MarkupLine("");
            AnsiConsole.MarkupLine("[blue]下一步:[/]");
            AnsiConsole.MarkupLine($"  1. 编辑 [yellow]{cfgPath}[/] 设置你的 API 密钥");
            AnsiConsole.MarkupLine("  2. 或设置 MYCLAW_API_KEY 环境变量");
            AnsiConsole.MarkupLine($"  3. 在 [yellow]{skillsDir}[/] 下添加技能（可选）");
            AnsiConsole.MarkupLine("  4. 运行 '[yellow]myclaw agent -m \"你好\"[/]' 测试");
        });
    }

    /// <summary>
    /// 从项目 templates 目录同步模板文件到工作区
    /// </summary>
    private static void SyncTemplates(string workspacePath)
    {
        // 查找 templates 目录
        var templatesDir = FindTemplatesDirectory();
        
        if (string.IsNullOrEmpty(templatesDir))
        {
            AnsiConsole.MarkupLine("[yellow]警告: 未找到 templates 目录，跳过模板同步[/]");
            return;
        }

        AnsiConsole.MarkupLine($"[blue]从 {templatesDir} 同步模板...[/]");

        var templateFiles = new[] { "AGENTS.md", "SOUL.md", "IDENTITY.md", "USER.md", "TOOLS.md", "HEARTBEAT.md", "BOOTSTRAP.md", "SUBAGENT.md", "MEMORY.md" };
        var copiedCount = 0;
        var skippedCount = 0;

        foreach (var fileName in templateFiles)
        {
            var sourcePath = Path.Combine(templatesDir, fileName);
            
            if (!File.Exists(sourcePath))
            {
                continue; // 模板文件不存在则跳过
            }

            var targetPath = Path.Combine(workspacePath, fileName);
            
            if (File.Exists(targetPath))
            {
                AnsiConsole.MarkupLine($"[dim]已存在，跳过: {fileName}[/]");
                skippedCount++;
            }
            else
            {
                try
                {
                    File.Copy(sourcePath, targetPath);
                    AnsiConsole.MarkupLine($"[green]已复制: {fileName}[/]");
                    copiedCount++;
                }
                catch (Exception ex)
                {
                    AnsiConsole.MarkupLine($"[red]复制失败 {fileName}: {ex.Message}[/]");
                }
            }
        }

        AnsiConsole.MarkupLine($"[blue]模板同步完成: {copiedCount} 个复制, {skippedCount} 个跳过[/]");
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
