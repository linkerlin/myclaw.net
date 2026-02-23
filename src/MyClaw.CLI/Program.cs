using System;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using MyClaw.CLI.Commands;
using MyClaw.Core.Configuration;
using Spectre.Console;

namespace MyClaw.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        try
        {
            // 自动检测并初始化（排除特定命令）
            if (!ShouldSkipAutoInit(args))
            {
                await AutoInitializeAsync();
            }

            var rootCommand = new RootCommand("myclaw - 个人 AI 助手");

            // 添加子命令
            rootCommand.AddCommand(new AgentCommand());
            rootCommand.AddCommand(new GatewayCommand());
            rootCommand.AddCommand(new OnboardCommand());
            rootCommand.AddCommand(new StatusCommand());
            rootCommand.AddCommand(new SkillsCommand());
            rootCommand.AddCommand(new SyncTemplatesCommand());

            return await rootCommand.InvokeAsync(args);
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]错误: {ex.Message}[/]");
            return 1;
        }
    }

    /// <summary>
    /// 检查是否应该跳过自动初始化
    /// （onboard、sync-templates、help、version 等命令不需要自动初始化）
    /// </summary>
    private static bool ShouldSkipAutoInit(string[] args)
    {
        if (args.Length == 0) return false;

        var firstArg = args[0].ToLowerInvariant();
        var skipCommands = new[] { "onboard", "sync-templates", "help", "--help", "-h", "--version", "-v" };
        
        return skipCommands.Contains(firstArg);
    }

    /// <summary>
    /// 自动初始化：检测工作区是否存在，不存在则自动执行 onboard 和 sync-templates
    /// </summary>
    private static async Task AutoInitializeAsync()
    {
        var cfg = ConfigurationLoader.Load();
        var workspacePath = cfg.Agent.Workspace;

        // 检查工作区是否存在且包含核心文件
        var needsInit = !Directory.Exists(workspacePath) || 
                        !File.Exists(Path.Combine(workspacePath, "AGENTS.md"));

        if (!needsInit)
        {
            return; // 已初始化，无需操作
        }

        // 显示自动初始化提示
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[blue]═══════════════════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("[blue]  🤖 首次启动检测到，正在自动初始化 MyClaw...[/]");
        AnsiConsole.MarkupLine("[blue]═══════════════════════════════════════════════════[/]");
        AnsiConsole.WriteLine();

        // 执行初始化
        await ExecuteOnboardAsync();
        
        // 执行模板同步
        await ExecuteSyncTemplatesAsync();

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[green]═══════════════════════════════════════════════════[/]");
        AnsiConsole.MarkupLine("[green]  ✅ 自动初始化完成！[/]");
        AnsiConsole.MarkupLine("[green]═══════════════════════════════════════════════════[/]");
        AnsiConsole.WriteLine();
    }

    /// <summary>
    /// 执行 onboard 初始化逻辑
    /// </summary>
    private static async Task ExecuteOnboardAsync()
    {
        await Task.Run(() =>
        {
            var cfgDir = ConfigurationLoader.ConfigDir;
            var cfgPath = ConfigurationLoader.ConfigPath;

            Directory.CreateDirectory(cfgDir);

            // 如果配置文件不存在则创建默认配置
            if (!File.Exists(cfgPath))
            {
                var defaultConfig = MyClawConfiguration.Default();
                ConfigurationLoader.Save(defaultConfig);
                AnsiConsole.MarkupLine($"[green]✓[/] 已创建配置: [dim]{cfgPath}[/]");
            }

            // 加载配置以获取工作区
            var cfg = ConfigurationLoader.Load();
            var ws = cfg.Agent.Workspace;

            // 创建工作区目录
            Directory.CreateDirectory(ws);
            AnsiConsole.MarkupLine($"[green]✓[/] 已创建工作区: [dim]{ws}[/]");

            // 创建子目录
            var memoryDir = Path.Combine(ws, "memory");
            Directory.CreateDirectory(memoryDir);

            var skillsDir = string.IsNullOrEmpty(cfg.Skills.Dir) 
                ? Path.Combine(ws, "skills") 
                : cfg.Skills.Dir;
            Directory.CreateDirectory(skillsDir);
        });
    }

    /// <summary>
    /// 执行模板同步逻辑
    /// </summary>
    private static async Task ExecuteSyncTemplatesAsync()
    {
        await Task.Run(() =>
        {
            var cfg = ConfigurationLoader.Load();
            var workspacePath = cfg.Agent.Workspace;
            var templatesDir = FindTemplatesDirectory();

            if (string.IsNullOrEmpty(templatesDir))
            {
                AnsiConsole.MarkupLine("[yellow]⚠ 未找到 templates 目录，跳过模板同步[/]");
                return;
            }

            AnsiConsole.MarkupLine($"[blue]→[/] 从 templates 同步文件...");

            var templateFiles = new[] { "AGENTS.md", "SOUL.md", "IDENTITY.md", "USER.md", "TOOLS.md", "HEARTBEAT.md", "BOOTSTRAP.md", "SUBAGENT.md", "MEMORY.md" };
            var copiedCount = 0;

            foreach (var fileName in templateFiles)
            {
                var sourcePath = Path.Combine(templatesDir, fileName);
                
                if (!File.Exists(sourcePath))
                {
                    continue;
                }

                var targetPath = Path.Combine(workspacePath, fileName);
                
                if (!File.Exists(targetPath))
                {
                    try
                    {
                        File.Copy(sourcePath, targetPath);
                        AnsiConsole.MarkupLine($"[green]  ✓[/] {fileName}");
                        copiedCount++;
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]  ✗[/] {fileName}: {ex.Message}");
                    }
                }
            }

            if (copiedCount > 0)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] 已复制 {copiedCount} 个模板文件");
            }
        });
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
        
        for (int i = 0; i < 5; i++)
        {
            var templatesPath = Path.Combine(searchDir, "templates");
            if (Directory.Exists(templatesPath))
            {
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
