using System.CommandLine;
using System.IO;
using System.Linq;
using MyClaw.Core.Configuration;
using MyClaw.Skills;
using Spectre.Console;

namespace MyClaw.CLI.Commands;

/// <summary>
/// Skills 命令 - 管理技能
/// </summary>
public class SkillsCommand : Command
{
    public SkillsCommand() : base("skills", "检查配置的技能")
    {
        AddCommand(new SkillsListCommand());
        AddCommand(new SkillsInfoCommand());
        AddCommand(new SkillsCheckCommand());
        AddCommand(new SkillsHarvestCommand());
    }
}

public class SkillsHarvestCommand : Command
{
    public SkillsHarvestCommand() : base("harvest", "扫描外部 AI 规则并转换为内部技能")
    {
        var applyOption = new Option<bool>(
            aliases: new[] { "--apply" },
            description: "实际写入 skills 目录；默认只做 dry-run");
        var overwriteOption = new Option<bool>(
            aliases: new[] { "--overwrite" },
            description: "允许覆盖已存在的导入目标");
        var sourcesOption = new Option<string?>(
            aliases: new[] { "--sources" },
            description: "逗号分隔的来源列表：copilot,cursor,claude,windsurf");
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "以 JSON 格式输出");

        AddOption(applyOption);
        AddOption(overwriteOption);
        AddOption(sourcesOption);
        AddOption(jsonOption);

        this.SetHandler((bool apply, bool overwrite, string? sources, bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            var workspace = cfg.Agent.Workspace;
            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);
            var harvester = new SubstrateHarvester(workspace, skillDir);
            var result = harvester.Harvest(new SubstrateHarvestOptions
            {
                Apply = apply,
                OverwriteExisting = overwrite,
                Sources = string.IsNullOrWhiteSpace(sources)
                    ? new List<string>()
                    : sources.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList()
            });

            if (json)
            {
                Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(result, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                }));
                return;
            }

            AnsiConsole.WriteLine(result.ToDisplayString());
        }, applyOption, overwriteOption, sourcesOption, jsonOption);
    }
}

/// <summary>
/// Skills list 子命令
/// </summary>
public class SkillsListCommand : Command
{
    public SkillsListCommand() : base("list", "列出已加载的技能")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "以 JSON 格式输出");

        AddOption(jsonOption);

        this.SetHandler((bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);

            // 加载 Skills
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();
            var skills = manager.LoadedSkills;

            if (json)
            {
                var skillsJson = skills.Select(s => new
                {
                    name = s.Name,
                    description = string.IsNullOrEmpty(s.Description) ? "(无描述)" : s.Description,
                    keywords = s.Keywords,
                    hooks = s.Hooks.Select(hook => hook.ToFrontmatterValue())
                });

                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.list",
                    ok = true,
                    enabled = cfg.Skills.Enabled,
                    dir = skillDir,
                    loaded = skills.Count,
                    skills = skillsJson
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"技能: 已启用={cfg.Skills.Enabled}, 目录={skillDir}");

                if (!cfg.Skills.Enabled)
                {
                    AnsiConsole.MarkupLine("[yellow]技能已在配置中禁用[/]");
                    return;
                }

                AnsiConsole.MarkupLine($"已加载技能: {skills.Count}");

                if (skills.Count == 0)
                {
                    AnsiConsole.MarkupLine("未找到技能");
                }
                else
                {
                    foreach (var skill in skills)
                    {
                        var desc = string.IsNullOrEmpty(skill.Description) 
                            ? "(无描述)" 
                            : skill.Description;
                        AnsiConsole.MarkupLine($"- [blue]{skill.Name}[/]: {desc}");
                    }
                }
            }
        }, jsonOption);
    }
}

/// <summary>
/// Skills info 子命令
/// </summary>
public class SkillsInfoCommand : Command
{
    public SkillsInfoCommand() : base("info", "显示技能详情")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "以 JSON 格式输出");

        var nameArgument = new Argument<string>("name", "技能名称");

        AddOption(jsonOption);
        AddArgument(nameArgument);

        this.SetHandler((string name, bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            
            if (!cfg.Skills.Enabled)
            {
                AnsiConsole.MarkupLine("[red]技能已在配置中禁用[/]");
                return;
            }

            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();

            var skill = manager.GetSkill(name);
            if (skill == null)
            {
                if (json)
                {
                    Console.WriteLine($"{{ \"schemaVersion\": 1, \"command\": \"skills.info\", \"ok\": false, \"error\": \"技能未找到: {name}\" }}");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[red]技能未找到: {name}[/]");
                }
                return;
            }

            if (json)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.info",
                    ok = true,
                    name = skill.Name,
                    description = skill.Description,
                    dir = skillDir,
                    keywords = skill.Keywords,
                    source = skill.SourcePath,
                    preview = string.Join("\n", skill.Content.Split('\n').Take(8))
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"[blue]名称:[/] {skill.Name}");
                
                var desc = string.IsNullOrEmpty(skill.Description) 
                    ? "(无描述)" 
                    : skill.Description;
                AnsiConsole.MarkupLine($"[blue]描述:[/] {desc}");
                
                AnsiConsole.MarkupLine($"[blue]技能目录:[/] {skillDir}");
                
                if (skill.Keywords.Count == 0)
                {
                    AnsiConsole.MarkupLine("[blue]关键词:[/] (无)");
                }
                else
                {
                    AnsiConsole.MarkupLine($"[blue]关键词:[/] {string.Join(", ", skill.Keywords)}");
                }

                AnsiConsole.MarkupLine($"[blue]来源:[/] {skill.SourcePath}");
                
                var preview = string.Join("\n", skill.Content.Split('\n').Take(8));
                if (!string.IsNullOrEmpty(preview))
                {
                    AnsiConsole.MarkupLine("[blue]提示词预览:[/]");
                    AnsiConsole.MarkupLine(preview);
                }
            }
        }, nameArgument, jsonOption);
    }
}

/// <summary>
/// Skills check 子命令
/// </summary>
public class SkillsCheckCommand : Command
{
    public SkillsCheckCommand() : base("check", "检查技能目录和加载状态")
    {
        var jsonOption = new Option<bool>(
            aliases: new[] { "--json" },
            description: "以 JSON 格式输出");

        AddOption(jsonOption);

        this.SetHandler((bool json) =>
        {
            var cfg = ConfigurationLoader.Load();
            var skillDir = SkillsCommandHelpers.GetSkillsDir(cfg);

            if (!json)
            {
                AnsiConsole.MarkupLine($"技能: 已启用={cfg.Skills.Enabled}, 目录={skillDir}");
            }

            if (!cfg.Skills.Enabled)
            {
                if (json)
                {
                    Console.WriteLine(@"{ ""schemaVersion"": 1, ""command"": ""skills.check"", ""ok"": true, ""enabled"": false, ""result"": ""disabled"" }");
                }
                else
                {
                    AnsiConsole.MarkupLine("结果: [yellow]已禁用[/]");
                }
                return;
            }

            var skillFolders = 0;
            var missingSkillMd = new List<string>();

            if (Directory.Exists(skillDir))
            {
                var dirs = Directory.GetDirectories(skillDir);
                skillFolders = dirs.Length;
                missingSkillMd = dirs.Where(d => !File.Exists(Path.Combine(d, "SKILL.md")))
                    .Select(Path.GetFileName)
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList()!;
            }

            // 加载统计
            var manager = new SkillManager(skillDir);
            manager.LoadSkills();
            var loaded = manager.LoadedSkills.Count;

            if (json)
            {
                var result = System.Text.Json.JsonSerializer.Serialize(new
                {
                    schemaVersion = 1,
                    command = "skills.check",
                    ok = true,
                    enabled = cfg.Skills.Enabled,
                    dir = skillDir,
                    skillFolders,
                    loaded,
                    missingSkillMD = missingSkillMd,
                    result2 = "ok"
                }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });

                Console.WriteLine(result);
            }
            else
            {
                AnsiConsole.MarkupLine($"技能文件夹: {skillFolders}");
                AnsiConsole.MarkupLine($"已加载技能: {loaded}");
                if (missingSkillMd.Count > 0)
                {
                    AnsiConsole.MarkupLine($"[yellow]缺少 SKILL.md: {string.Join(", ", missingSkillMd)}[/]");
                }
                AnsiConsole.MarkupLine("结果: [green]正常[/]");
            }
        }, jsonOption);
    }
}

internal static class SkillsCommandHelpers
{
    public static string GetSkillsDir(MyClawConfiguration cfg)
    {
        return SkillPaths.ResolveSkillsDirectory(cfg.Agent.Workspace, cfg.Skills.Dir);
    }
}
